from websockets.sync.client import connect, ClientConnection
from websockets.exceptions import *
import json

import src.exception as ex
from src.cancellation import CancellationObject

class RecognitionOutputer:
    def output(self, text: str):
        ...

class PrintOutputer:
    def output(self, text: str):
        print(text)

class WebSocketOutputer(RecognitionOutputer):
    def __init__(self, uri:str, _:CancellationObject):
        self.uri = uri
        self.soc:ClientConnection | None = None
        try:
            self.__con()
        except:
            # コンストラクタなので今回は無視する
            pass

    def __del__(self):
        if not self.soc is None:
            try:
                self.soc.close()
            except:
                # どうしようもないし無視する
                pass
        self.soc = None

    def __con(self) -> None:
        if not self.soc is None:
            return

        self.soc = connect(self.uri)


    def output(self, text:str):
        """
        async def send(text:str):
            async with websockets.connect(self.uri) as websocket:
                await websocket.send(text)
                await websocket.close()
        """
        print(text)
        try:
            self.__con()
        except Exception as e:
            self.soc = None
            raise WsOutputException("リモートへの接続に失敗しました", e)
        try:
            if isinstance(self.soc, ClientConnection):
                self.soc.send(text)
        except Exception as e:
            self.soc = None
            raise WsOutputException("リモートへの送信に失敗しました", e)


# ゆかりねっとws仕様
# 0:[認識文字列] => 認識結果の文字列を送信
# 1:close
# 1:[エラーメッセージ]
# 2:network
# 3:aborted
class YukarinetteOutputer(WebSocketOutputer):
    def __init__(self, uri:str, cancel:CancellationObject):
        super().__init__(uri, cancel)

    def output(self, text:str):
        super().output(f"0:{text}")

class YukaconeOutputer(WebSocketOutputer):
    def __init__(self, uri:str, cancel:CancellationObject):
        super().__init__(f"{uri}/textonly", cancel)

    def output(self, text:str):
        super().output(text)

class IlluminateSpeechOutputer(WebSocketOutputer):
    def __init__(self, uri:str, cancel:CancellationObject):
        super().__init__(uri, cancel)

    def output(self, text:str):
        super().output(json.dumps({
            "transcript": text
        }))

class WsOutputException(ex.IlluminateException):
    pass
