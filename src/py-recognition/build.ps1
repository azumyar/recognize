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


echo gitのインストール確認
Get-Command git > $null
if(-not($?)) {
    echo gitが見つからないのでgitのインストールを行います
    winget install git.git
    if($LASTEXITCODE -ne 0) {
        echo インストールが失敗またはキャンセルされました
        exit 1
    }
}
echo ok
echo ""

$env:PIPENV_VENV_IN_PROJECT = 1
#$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

echo python環境にpipenvをインストールします
pip3 install pipenv
if($LASTEXITCODE -ne 0) {
    echo インストールに失敗しました
    exit 1
}
echo ok
echo ""

echo 仮想環境を作成しpython依存関係を復元します
python -m pipenv install
if($LASTEXITCODE -ne 0) {
    echo python依存関係の復元に失敗しました
    exit 1
}
python -m pipenv install --dev
if($LASTEXITCODE -ne 0) {
    echo python依存関係の復元に失敗しました
    exit 1
}
echo ok
echo ""

echo exe化を実行します
python -m pipenv run archive1
if($LASTEXITCODE -ne 0) {
    echo exe化に失敗しました
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
    exit 1
}
echo ok
echo ""

echo 正常に終了しました
echo ""

echo ""
echo "ビルドされたexeの場所："
echo "$cd\dist\recognize"
exit 0