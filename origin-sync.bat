if exist ".py-recognition-origin" (
  cd .py-recognition-origin
  git pull
  cd ..
) else (
  git clone --filter=blob:none --no-checkout --depth=1 https://github.com/azumyar/illuminate-speech.git .py-recognition-origin
  cd .py-recognition-origin
  git sparse-checkout set src/py-recognition
  git checkout
  cd ..
)
robocopy .py-recognition-origin\src\py-recognition py-recognition /mir /xd .git