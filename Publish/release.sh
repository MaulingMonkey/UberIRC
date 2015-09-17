export OWNER=MaulingMonkey
export REPO=UberIRC
export SEVENZIP="/c/Program Files/7-Zip/7z"
export ARCHIVE=UberIRC-$1.zip
export VERSION=$1

# set -o verbose
pushd $(dirname $0)

if [ "$1" = "" ]; then
	echo Usage: $0 v0.0.?
else
	# Prepare archive
	echo
	"${SEVENZIP}" a ${ARCHIVE} @release-filelist.txt
	ls ${ARCHIVE}

	# Create a release
	echo
	echo tmp-release-request.txt:
	cat template-release.txt | sed -e s/REPLACE_WITH_TAG_NAME/${VERSION}/g | tee tmp-release-request.txt
	echo
	echo curl https://api.github.com/repos/${OWNER}/${REPO}/releases ...
	curl https://api.github.com/repos/${OWNER}/${REPO}/releases \
		--request POST \
		-H "Content-Type: application/json" \
		-H "$(cat ~/.gitpublish)" \
		--data @tmp-release-request.txt \
		| tee tmp-release-response.txt

	# Get release ID
	export RELEASEID=$(sed -nr 's/\s*\"id\": ([0-9]+),\s*/\1/p' tmp-release-response.txt | head -n 1)

	# Upload to release
	echo curl https://uploads.github.com/repos/${OWNER}/${REPO}/releases/${RELEASEID}/assets?name=${ARCHIVE} ...
	curl https://uploads.github.com/repos/${OWNER}/${REPO}/releases/${RELEASEID}/assets?name=${ARCHIVE} \
		--request POST \
		-H "Content-Type: application/zip" \
		-H "$(cat ~/.gitpublish)" \
		--data-binary @${ARCHIVE} \
		| tee tmp-upload-response.txt


	# Cleanup & Sync
	rm tmp-*.txt
	git fetch github ${VERSION}
fi

popd
# set +o verbose
