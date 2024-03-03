echo pythonのインストール確認
Get-Command pip3 > $null
if (-not($?)) {
    echo pipが見つからないのでpythonのインストールを行います
    winget install python3.10
    if($LASTEXITCODE -ne 0) {
        echo インストールが失敗またはキャンセルされました
        exit 1
    }
}
echo ok
echo ""

echo pythonのバージョン確認
$py_v = python -V | ConvertFrom-String -Delimiter '[\s\.]+'
# python 3.10.x以上を要求
if(($py_v.P2 -ne 3) -or (($py_v.P2 -eq 3) -and ($py_v.P3 -lt 10))) {
    echo エラー
    echo インストールされているpythonはサポートされていません
    python -V
    Get-Command python | Select-Object Source | Format-Table -AutoSize -Wrap
    exit 1
}
echo ok
echo ""


#echo gitのインストール確認
#Get-Command git > $null
#if(-not($?)) {
#    echo gitが見つからないのでgitのインストールを行います
#    winget install git.git
#    if($LASTEXITCODE -ne 0) {
#        echo インストールが失敗またはキャンセルされました
#        exit 1
#    }
#}
#echo ok
#echo ""

echo 作業ディレクトリの準備を行います
If (Test-Path .\.build) {
    echo 古い作業ディレクトリを削除します
    Remove-Item -path .\.build -recurse
    if($LASTEXITCODE -ne 0) {
        echo 古い作業ディレクトリの削除に失敗しました
        exit 1
    }
    echo ok
    echo ""
}

mkdir .build
if($LASTEXITCODE -ne 0) {
    echo 作業ディレクトリの作成に失敗しました
    exit 1
}
Copy-Item -Path .\src -Destination .\.build.\src -Recurse
if($LASTEXITCODE -ne 0) {
    echo ソースコードの複製に失敗しました
    exit 1
}
Copy-Item -Path .\Pipfile.build -Destination .\.build.\Pipfile
if($LASTEXITCODE -ne 0) {
    echo Pipfileの複製に失敗しました
    exit 1
}


pushd .build


$env:PIPENV_VENV_IN_PROJECT = 1
#$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

echo python環境にpipenvをインストールします
pip3 install pipenv
if($LASTEXITCODE -ne 0) {
    echo インストールに失敗しました
    popd
    exit 1
}
echo ok
echo ""

echo 仮想環境を作成しpython依存関係を復元します
python -m pipenv install --dev --skip-lock
if($LASTEXITCODE -ne 0) {
    echo python依存関係の復元に失敗しました
    popd
    exit 1
}

python -m pipenv shell pip install -r ../requirements.txt --no-cache-dir
if($LASTEXITCODE -ne 0) {
    echo python依存関係の復元に失敗しました
    popd
    exit 1
}

# CUDA関連が容量を圧迫するのでキャッシュは削除する
# python -m pipenv shell pip cache purge
echo ok
echo ""

echo exe化を実行します
python -m pipenv run archive1
if($LASTEXITCODE -ne 0) {
    echo exe化に失敗しました
    popd
    exit 1
}
$cd = Get-Location
echo "$cd\dist\recognize に作成しました"
echo ok
echo ""

echo 仮想環境を削除します
python -m pipenv --rm
if($LASTEXITCODE -ne 0) {
    echo 削除に失敗しました
    popd
    exit 1
}
echo ok
echo ""


popd


echo アーカイブを移動します
If (Test-Path .\dist) {
    echo 既存のアーカイブを削除します
    Remove-Item -path .\dist -recurse
    if($LASTEXITCODE -ne 0) {
        echo 既存のアーカイブの削除に失敗しました
        exit 1
    }
}
move .build\dist .
if($LASTEXITCODE -ne 0) {
    echo アーカイブの移動に失敗しました
    exit 1
}

echo 作業ディレクトリを削除します
Remove-Item  -path .build -recurse
if($LASTEXITCODE -ne 0) {
    echo 作業ディレクトリの削除に失敗しました
    exit 1
}

echo 正常に終了しました
echo ""

$cd = Get-Location
echo ""
echo "ビルドされたexeの場所："
echo "$cd\dist\recognize"
exit 0