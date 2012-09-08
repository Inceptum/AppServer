o build-wrap -Incremental

cd TestData\TestApp\
del TestApp-*.wrap
o update-wrap -From ../../ -UseSystem
o build-wrap -Incremental
cd ../../
del TestApp-*.wrap
copy .\TestData\TestApp\TestApp-*.wrap .

