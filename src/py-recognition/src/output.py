from websockets.sync.client import connect, ClientConnection
import websockets.exceptions
import json

import src.exception as ex
from src.cancellation import CancellationObject

class RecognitionOutputer:
    """
    認識結果を出力する抽象基底クラス
    """
    def output(self, text: str) -> None:
        """
        認識結果を出力
        """
        ...

class PrintOutputer:
    """
    標準出力に出力する
    """
    def output(self, text: str):
        print(text)

class WebSocketOutputer(RecognitionOutputer):
    """
    ウェブソケットに出力する基底クラス
    """
    def __init__(self, uri:str, remote_name:str, _:CancellationObject):
        self.__uri = uri
        self.__remote_name = remote_name
        self.__soc:ClientConnection | None = None
        try:
            self.__con()
        except:
            # コンストラクタなので今回は無視する
            pass

    def __del__(self):
        if not self.__soc is None:
            try:
                self.__soc.close()
            except:
                # どうしようもないし無視する
                pass
        self.__soc = None

    def __con(self) -> None:
        """
        ウェブソケットに接続を試行する
        """
        if not self.__soc is None:
            return

        self.__soc = connect(self.__uri)


    def output(self, text:str):        
        #async def send(text:str):
        #    async with websockets.connect(self.uri) as websocket:
        #        await websocket.send(text)
        #        await websocket.close()
        print(text)
        try:
            self.__con()
        except Exception as e:
            self.__soc = None
            raise WsOutputException(f"リモート[{self.__remote_name}]への接続に失敗しました", e)

        try:
            if isinstance(self.__soc, ClientConnection):
                self.__soc.send(text)
        except Exception as e:
            self.__soc = None
            raise WsOutputException(f"リモート[{self.__remote_name}]への送信に失敗しました", e)


# ゆかりねっとws仕様
# 0:[認識文字列] => 認識結果の文字列を送信
# 1:close
# 1:[エラーメッセージ]
# 2:network
# 3:aborted
class YukarinetteOutputer(WebSocketOutputer):
    """
    ゆかりねっと外部連携に出力する
    """
    def __init__(self, uri:str, cancel:CancellationObject):
        super().__init__(uri, "ゆかりねっと", cancel)

    def output(self, text:str):
        super().output(f"0:{text}")

class YukaconeOutputer(WebSocketOutputer):
    """
    ゆかコネNEO外部連携に出力する
    """
    def __init__(self, uri:str, cancel:CancellationObject):
        super().__init__(f"{uri}/textonly", "ゆかコネNEO", cancel)

    def output(self, text:str):
        super().output(text)

class IlluminateSpeechOutputer(WebSocketOutputer):
    def __init__(self, uri:str, cancel:CancellationObject):
        super().__init__(uri, "-", cancel)

    def output(self, text:str):
        super().output(json.dumps({
            "transcript": text
        }))

class WsOutputException(ex.IlluminateException):
    """
    ウェブソケットでエラーが出た際になげる例外
    """
    pass
