echo pythonのインストール確認
Get-Command pip3 > $null
if (-not($?)) {
    echo pipが見つからないのでpythonのインストールを行います
    winget install python3.10
    if($LASTEXITCODE -ne 0) {
        echo インストールが失敗またはキャンセルされました
        exit
    }
}
echo ok

echo gitのインストール確認
Get-Command git > $null
if(-not($?)) {
    echo gitが見つからないのでgitのインストールを行います
    winget install git.git
    if($LASTEXITCODE -ne 0) {
        echo インストールが失敗またはキャンセルされました
        exit
    }
}
echo ok

$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

echo python環境にpipenvをインストールします
pip3 install pipenv
if($LASTEXITCODE -ne 0) {
    echo インストールに失敗しました
    exit
}
echo ok

echo 仮想環境を作成しpython依存関係を復元します
python -m pipenv install
if($LASTEXITCODE -ne 0) {
    echo python依存関係の復元に失敗しました
    exit
}
python -m pipenv install --dev
if($LASTEXITCODE -ne 0) {
    echo python依存関係の復元に失敗しました
    exit
}
echo ok

echo exe化を実行します
python -m pipenv run archive1
if($LASTEXITCODE -ne 0) {
    echo exe化に失敗しました
    exit
}
$cd = Get-Location
echo "$cd\dist\recognize に作成しました"
echo ok

echo 仮想環境を削除します
python -m pipenv --rm
if($LASTEXITCODE -ne 0) {
    echo 削除に失敗しました
    exit
}
echo ok

echo 正常に終了しました

echo ""
echo "ビルドされたexeの場所："
echo "$cd\dist\recognize"