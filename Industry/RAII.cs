// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Industry {
	// http://msdn.microsoft.com/en-us/library/system.reflection.emit.dynamicmethod.aspx

	public class Owns : Attribute {
		public readonly bool Key, Value;

		public Owns() { Key = Value = true; }
		public Owns( bool value ) { Key = Value = value; }
		public Owns( bool key, bool value ) { Key = key; Value = value; }
	}

	namespace Internal {
		/// <summary>
		/// We have to make these public since the generated IL doesn't belong to RAII or RAIIMethods and can reside in an entirely different assembly
		/// </summary>
		public static class RAIIMethods {
			public static void DisposeOfDictionary( IDictionary dictionary, bool keys, bool values ) {
				foreach ( DictionaryEntry keyval in dictionary ) {
					IDisposable disposable;
					if ( keys   && (disposable = keyval.Key   as IDisposable) != null ) disposable.Dispose();
					if ( values && (disposable = keyval.Value as IDisposable) != null ) disposable.Dispose();
				}
			}

			public static void DisposeOfEnumerable( IEnumerable enumerable ) {
				IDisposable disposable;
				foreach ( object e in enumerable ) if ( (disposable = e as IDisposable) != null ) disposable.Dispose();
			}
		}
	}

	public class RAII : IDisposable {
		public void Dispose() { Dispose(this); }

		delegate void Disposer( object todispose );
		static Dictionary<Type,Disposer> disposers = new Dictionary<Type,Disposer>();

		static void CheckMarkedAsOwned( Owns o, FieldInfo field ) {
			Debug.Assert
				( o != null
				, "Field "+field+" of "+field.DeclaringType+" is not marked with Industry.Owns(true|false)"
				, "The field is of a static type of *, IEnumerable<*>, or IDictionary<*>"
				+ " (where * implements IDisposable) but is not marked with the attribute Industry.Owns."
				);
		}

		class DisposerCreator {
			Type          type;
			DynamicMethod method;
			ILGenerator   il;
			BindingFlags  flags;

			public DisposerCreator( Type type ) {
				this.type    = type;
				this.method  = new DynamicMethod( "Dispose", null, new[] { typeof(object) }, type );
				this.il      = method.GetILGenerator();
				this.flags   = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField;
			}

			public Disposer Create() {
				foreach ( var field in type.GetFields(flags) ) {
					var attribs = field.GetCustomAttributes( typeof(Owns), false );
					Debug.Assert( attribs.Length <= 1 );

					var FieldType = field.FieldType;
					Owns owns = null;
					if ( attribs.Length != 0 ) owns = attribs[0] as Owns;
					else if ( type.IsSubclassOf( field.DeclaringType ) ) owns = new Owns(false);

					if ( field.FieldType.GetInterface(typeof(IDisposable).FullName) != null )        EmitDisposeForIDisposable( field, owns );
					else if ( field.FieldType.GetInterface(typeof(IDictionary).FullName) != null )   EmitDisposeForIDictionary( field, owns );
					else if ( field.FieldType.GetInterface(typeof(IEnumerable).FullName) != null )   EmitDisposeForIEnumerable( field, owns );
					else if ( owns != null && ( owns.Key || owns.Value ) )                           EmitDisposeFor_Failed    ( field, owns );
				}
				il.Emit( OpCodes.Ret );
				return method.CreateDelegate(typeof(Disposer)) as Disposer;
			}

			void EmitDisposeFor_Failed( FieldInfo field, Owns owns ) {
				throw new InvalidOperationException( "Member "+field.Name+" is not an IDisposable, IDictionary, or IEnumerable but is marked with [Owns]" );
			}

			void EmitDisposeForIDisposable( FieldInfo field, Owns owns ) {
				CheckMarkedAsOwned( owns, field );
				if ( owns.Key == false ) return; // skip emitting anything for this member

				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldfld, field );
				il.Emit( OpCodes.Ldnull );
				il.Emit( OpCodes.Ceq );
				Label isnull = il.DefineLabel();
				il.Emit( OpCodes.Brtrue, isnull );
				
				var dispose = typeof(IDisposable).GetMethod( "Dispose" );
				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldfld, field );
				il.EmitCall( OpCodes.Call, dispose, null );
				
				il.MarkLabel( isnull );
			}

			void EmitDisposeForIDictionary( FieldInfo field, Owns owns ) {
				Type key = null;
				Type value = null;

				foreach ( var iface in field.FieldType.GetInterfaces() )
				if ( iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(Dictionary<,>) )
				{
					var args = iface.GetGenericArguments();
					key   = args[0];
					value = args[1];
					break;
				}
				
				if ( key != null || value != null ) {
					// We were able to infer generic interface parameters

					var keyIsDisposable   = key  .GetInterface(typeof(IDisposable).FullName) != null;
					var valueIsDisposable = value.GetInterface(typeof(IDisposable).FullName) != null;
					
					if ( !keyIsDisposable && !valueIsDisposable ) return; // skip emitting anything for this member
					CheckMarkedAsOwned( owns, field );
					if ( owns.Key == false && owns.Value == false ) return; // skip emitting anything for this member
				} else if ( owns == null || (!owns.Key && !owns.Value) ) {
					return; // We weren't able to infer generic interface parameters AND the member isn't marked as Owned.  Skip it.
				}

				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldfld, field );
				il.Emit( OpCodes.Ldnull );
				il.Emit( OpCodes.Ceq );
				Label isdictnull = il.DefineLabel();
				il.Emit( OpCodes.Brtrue, isdictnull );

				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldfld, field );
				il.Emit( owns.Key  ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0 );
				il.Emit( owns.Value? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0 );
				il.EmitCall( OpCodes.Call, typeof(Internal.RAIIMethods).GetMethod("DisposeOfDictionary", BindingFlags.Public | BindingFlags.Static), null );

				il.MarkLabel( isdictnull );
			}

			void EmitDisposeForIEnumerable( FieldInfo field, Owns owns ) {
				Type value = null;

				foreach ( var iface in field.FieldType.GetInterfaces() )
				if ( iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>) )
				{
					var args = iface.GetGenericArguments();
					value = args[0];
					break;
				}

				if ( value != null ) {
					// We were able to infer generic interface parameters

					var valueIsDisposable = value.GetInterface(typeof(IDisposable).FullName) != null;
					//var valueIsDisposable = value.IsSubclassOf(typeof(IDisposable));
					
					if ( !valueIsDisposable ) return; // skip emitting anything for this member
					CheckMarkedAsOwned( owns, field );
					if ( owns.Key == false ) return; // skip emitting anything for this member
				} else if ( owns == null || !owns.Key ) {
					return; // We weren't able to infer generic interface parameters AND the member isn't marked as Owned.  Skip it.
				}
				
				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldfld, field );
				il.Emit( OpCodes.Ldnull );
				il.Emit( OpCodes.Ceq );
				Label islistnull = il.DefineLabel();
				il.Emit( OpCodes.Brtrue, islistnull );

				il.Emit( OpCodes.Ldarg_0 );
				il.Emit( OpCodes.Ldfld, field );
				il.EmitCall( OpCodes.Call, typeof(Internal.RAIIMethods).GetMethod("DisposeOfEnumerable", BindingFlags.Public | BindingFlags.Static), null );

				il.MarkLabel( islistnull );
			}
		}

		static Disposer CreateDisposeMethodFor( Type type ) {
			if ( disposers.ContainsKey(type) ) return disposers[type]; // already exists
			return disposers[type] = new DisposerCreator(type).Create();
		}

		public static void Dispose( object self ) {
			var type = self.GetType();
			var disposer = CreateDisposeMethodFor(type);
			disposer.Invoke(self);
		}
	}
}
