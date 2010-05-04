// Copyright Michael B. E. Rickert 2009
// Distributed under the Boost Software License, Version 1.0.
// (See accompanying file ..\LICENSE.txt or copy at http://www.boost.org/LICENSE.txt)

using System;
using System.Collections.Generic;
using Industry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
	[TestClass]
	public class RAIITests {
		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext {
			get {
				return testContextInstance;
			}
			set {
				testContextInstance = value;
			}
		}

		#region Additional test attributes
		//
		// You can use the following additional attributes as you write your tests:
		//
		// Use ClassInitialize to run code before running the first test in the class
		// [ClassInitialize()]
		// public static void MyClassInitialize(TestContext testContext) { }
		//
		// Use ClassCleanup to run code after all tests in a class have run
		// [ClassCleanup()]
		// public static void MyClassCleanup() { }
		//
		// Use TestInitialize to run code before running each test 
		// [TestInitialize()]
		// public void MyTestInitialize() { }
		//
		// Use TestCleanup to run code after each test has run
		// [TestCleanup()]
		// public void MyTestCleanup() { }
		//
		#endregion

		
		class DisposableMock : IDisposable { public bool WasDisposed; public void Dispose() { WasDisposed = true; } }

		class DisposableRAII : RAII {
			[Owns       ] public DisposableMock d1 = new DisposableMock();
			[Owns(true )] public DisposableMock d2 = new DisposableMock();
			[Owns(false)] public DisposableMock d3 = new DisposableMock();

			[Owns       ] public List<DisposableMock> dl1 = new List<DisposableMock>() { new DisposableMock() };
			[Owns(true )] public List<DisposableMock> dl2 = new List<DisposableMock>() { new DisposableMock() };
			[Owns(false)] public List<DisposableMock> dl3 = new List<DisposableMock>() { new DisposableMock() };

			[Owns       ] public Dictionary<DisposableMock,DisposableMock> di1 = new Dictionary<DisposableMock,DisposableMock>() { { new DisposableMock(), new DisposableMock() } };
			[Owns(true )] public Dictionary<DisposableMock,DisposableMock> di2 = new Dictionary<DisposableMock,DisposableMock>() { { new DisposableMock(), new DisposableMock() } };
			[Owns(false)] public Dictionary<DisposableMock,DisposableMock> di3 = new Dictionary<DisposableMock,DisposableMock>() { { new DisposableMock(), new DisposableMock() } };

			[Owns(false,false)] public Dictionary<DisposableMock,DisposableMock> di4 = new Dictionary<DisposableMock,DisposableMock>() { { new DisposableMock(), new DisposableMock() } };
			[Owns(false,true )] public Dictionary<DisposableMock,DisposableMock> di5 = new Dictionary<DisposableMock,DisposableMock>() { { new DisposableMock(), new DisposableMock() } };
			[Owns(true ,false)] public Dictionary<DisposableMock,DisposableMock> di6 = new Dictionary<DisposableMock,DisposableMock>() { { new DisposableMock(), new DisposableMock() } };
			[Owns(true ,true )] public Dictionary<DisposableMock,DisposableMock> di7 = new Dictionary<DisposableMock,DisposableMock>() { { new DisposableMock(), new DisposableMock() } };
		}

		DisposableRAII d = new DisposableRAII();
		[TestInitialize()] public void DisposeOfD() { d.Dispose(); }
		
		[TestMethod] public void TestIDisposables() {
			Assert.IsTrue ( d.d1.WasDisposed );
			Assert.IsTrue ( d.d2.WasDisposed );
			Assert.IsFalse( d.d3.WasDisposed );
		}

		[TestMethod] public void TestIEnumerables() {
			Assert.IsTrue ( d.dl1[0].WasDisposed );
			Assert.IsTrue ( d.dl2[0].WasDisposed );
			Assert.IsFalse( d.dl3[0].WasDisposed );
		}

		[TestMethod] public void TestIDictionarys() {
			foreach ( var entry in d.di1 ) Assert.IsTrue( entry.Key.WasDisposed && entry.Value.WasDisposed );
			foreach ( var entry in d.di2 ) Assert.IsTrue( entry.Key.WasDisposed && entry.Value.WasDisposed );
			foreach ( var entry in d.di3 ) Assert.IsTrue(!entry.Key.WasDisposed &&!entry.Value.WasDisposed );
			foreach ( var entry in d.di4 ) Assert.IsTrue(!entry.Key.WasDisposed &&!entry.Value.WasDisposed );
			foreach ( var entry in d.di5 ) Assert.IsTrue(!entry.Key.WasDisposed && entry.Value.WasDisposed );
			foreach ( var entry in d.di6 ) Assert.IsTrue( entry.Key.WasDisposed &&!entry.Value.WasDisposed );
			foreach ( var entry in d.di7 ) Assert.IsTrue( entry.Key.WasDisposed && entry.Value.WasDisposed );
		}
	}
}
