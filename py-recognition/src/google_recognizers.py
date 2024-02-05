from __future__ import annotations

import json
import random as rnd
import requests
from urllib.parse import urlencode
from urllib.request import Request, urlopen
from typing import Any, Callable, NamedTuple
from concurrent.futures import ThreadPoolExecutor

from speech_recognition.audio import AudioData

import src.exception as exception

__server_url_recognize = "https://www.google.com/speech-api/v2/recognize"
"""google音声認識v2 APIエントリ"""

__server_url_full_duplex = "https://www.google.com/speech-api/full-duplex/v1"
"""google全二重APIエントリベース"""

__charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
"""google全二重API識別子に使用する文字一覧"""

__api_key = "AIzaSyBOti4mM-6x9WDnZIjIeyEU21OpBXqWBgw"
"""APIキー"""

__user_agent  = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36"
"""ユーザエージェント"""

__session = requests.session()

recognize_google:Callable[[EncodeData, float|None, str|None, str|None, int], RecognizeResult]
"""
google音声認識API v2を用いて音声認識
"""
recognize_google_duplex:Callable[[EncodeData, float|None, str|None, str|None, int], RecognizeResult]
"""
google全二重APIを用いて音声認識
"""

class EncodeData(NamedTuple):
    """
    エンコード結果が入るデータクラス
    """
    audio:bytes
    content_type:str

class RecognizeResult(NamedTuple):
    """
    認識関数の戻り値を返すデータクラス
    """
    transcript:str
    raw_data:str


class DuplexApiResult(NamedTuple):
    """
    全二重APIの戻り値を返すデータクラス
    """
    sucess:bool
    transcript:str
    exception:Exception | None


def encode_falc(audio_data:AudioData, convert_rate:int | None) -> EncodeData:
    """
    FALCにエンコード
    """
    conv = convert_rate
    if (not conv is None and conv < 8000) or (conv is None and audio_data.sample_rate < 8000):
        conv = 8000
    flac_data = audio_data.get_flac_data(
        convert_rate = conv,
        convert_width = 2 
    )
    return EncodeData(flac_data, f"audio/x-flac; rate={conv if not conv is None else audio_data.sample_rate}")


def recognize_google_urllib(audio_data:EncodeData, timeout:float | None, key:str | None=None, language:str="en-US", pfilter:int=0) -> RecognizeResult:
    """
    google音声認識API v2を用いて音声認識
    """
    if key is None:
        key = __api_key
    url = f"{__server_url_recognize}?{{}}".format(urlencode({
        "client": "chromium",
        "lang": language,
        "key": key,
        "pFilter": pfilter
    }))
    request = Request(
        url,
         data=audio_data.audio, 
         headers= {
            "Content-Type": audio_data.content_type,
            "User-Agent": __user_agent
        })
    with urlopen(request, timeout=timeout) as response:
        return __parse(response.read().decode("utf-8"))


def recognize_google_requests(audio_data:EncodeData, timeout:float | None, key:str | None=None, language:str="en-US", pfilter:int=0) -> RecognizeResult:
    """
    google音声認識API v2を用いて音声認識
    """
    if key is None:
        key = __api_key
    url = f"{__server_url_recognize}?{{}}".format(urlencode({
        "client": "chromium",
        "lang": language,
        "key": key,
        "pFilter": pfilter
    }))
    res = __session.post(
        url,
        data=audio_data.audio, 
        headers= {
           "Content-Type": audio_data.content_type,
            "User-Agent": __user_agent
        },
        timeout=timeout)
    if res.status_code != 200:
        raise HttpStatusError(f"HTTPリクエストは{res.status_code}で失敗しました", res.status_code)
    return __parse(res.content.decode("utf-8"))


def recognize_google_duplex_urllib(audio_data:EncodeData, timeout:float | None, key:str | None=None, language:str="en-US", pfilter:int=0) -> RecognizeResult:
    """
    google全二重APIを用いて音声認識
    """
    def generate_pair() -> str:
        """
        識別用ペアを生成
        """
        b = []
        for i in [rnd.randint(0, len(__charset)-1) for _ in range(16)]:
            b.append(__charset[i])
        return "".join(b)

    def up(pair:str, audio_data:EncodeData, timeout:float | None, key:str | None=None, language:str="en-US", pfilter:int=0) -> None:
        """
        up APIを呼び出し
        """
        request = Request(
            f"{__server_url_full_duplex}/up?{{}}".format(urlencode({
                "key": key,
                "pair": pair,
                "output": "json",
                "app": "chromium",
                "lang": language,
                "pFilter": pfilter,
            })),
            data = audio_data.audio,
            headers = {
                "Content-Type": audio_data.content_type,
                "User-Agent": __user_agent
            },
            method = "POST")
        with urlopen(request, timeout=timeout) as _:
            pass

    def down(pair:str, timeout:float | None, key:str | None) -> DuplexApiResult:
        """
        down APIを呼び出し
        """
        try:
            request = Request(
                f"{__server_url_full_duplex}/down?{{}}".format(urlencode({
                    "key": key,
                    "pair": pair,
                    "output": "json",
                })),
                headers = {
                    "User-Agent": __user_agent
                },
                method = "GET")
            with urlopen(request, timeout=timeout) as response:
                response_text = response.read().decode("utf-8")
                return DuplexApiResult(True, response_text, None)
        except Exception as e:
            return DuplexApiResult(False, "", e)
    if key is None:
        key = __api_key

    pair = generate_pair()
    thread_pool = ThreadPoolExecutor(max_workers=6)
    try:
        future = thread_pool.submit(down, pair, timeout, key)
        thread_pool.submit(up, pair, audio_data, timeout, key, language, pfilter)

        r = future.result()
        if r.sucess:
            return __parse(r.transcript)
        else:
            assert not r.exception is None
            if not r.exception is None:
                raise r.exception
        raise exception.ProgramError()
    finally:
        thread_pool.shutdown(wait=False)

def recognize_google_duplex_requests(audio_data:EncodeData, timeout:float | None, key:str | None=None, language:str="en-US", pfilter:int=0) -> RecognizeResult:
    """
    google全二重APIを用いて音声認識
    """
    def generate_pair() -> str:
        """
        識別用ペアを生成
        """
        b = []
        for i in [rnd.randint(0, len(__charset)-1) for _ in range(16)]:
            b.append(__charset[i])
        return "".join(b)

    def up(pair:str, audio_data:EncodeData, timeout:float | None, key:str | None=None, language:str="en-US", pfilter:int=0) -> None:
        """
        up APIを呼び出し
        """
        __session.post(
            f"{__server_url_full_duplex}/up?{{}}".format(urlencode({
                "key": key,
                "pair": pair,
                "output": "json",
                "app": "chromium",
                "lang": language,
                "pFilter": pfilter,
            })),
            data = audio_data.audio,
            headers = {
                "Content-Type": audio_data.content_type,
                "User-Agent": __user_agent
            },
            timeout=timeout)

    def down(pair:str, timeout:float | None, key:str | None) -> DuplexApiResult:
        """
        down APIを呼び出し
        """
        try:
            res = __session.get(
                f"{__server_url_full_duplex}/down?{{}}".format(urlencode({
                    "key": key,
                    "pair": pair,
                    "output": "json",
                })),
                headers = {
                    "User-Agent": __user_agent
                },
                timeout=timeout)
            if res.status_code == 200:
                response_text = res.content.decode("utf-8")
                return DuplexApiResult(True, response_text, None)
            else:
                return DuplexApiResult(False, "", HttpStatusError(f"HTTPリクエストは{res.status_code}で失敗しました", res.status_code))
        except Exception as e:
            return DuplexApiResult(False, "", e)
    if key is None:
        key = __api_key

    pair = generate_pair()
    thread_pool = ThreadPoolExecutor(max_workers=6)
    try:
        future = thread_pool.submit(down, pair, timeout, key)
        thread_pool.submit(up, pair, audio_data, timeout, key, language, pfilter)

        r = future.result()
        if r.sucess:
            return __parse(r.transcript)
        else:
            assert not r.exception is None
            if not r.exception is None:
                raise r.exception
        raise exception.ProgramError()
    finally:
        thread_pool.shutdown(wait=False)


def __parse(response_text:str) -> RecognizeResult:
    """
    APIの戻り値をパース
    """
    actual_result = []
    for line in response_text.split("\n"):
        if not line: continue
        result = json.loads(line)["result"]
        if len(result) != 0:
            for r in result:
                actual_result.append(r)

    if len(actual_result) == 0:
        #or not isinstance(actual_result[0], dict) or len(actual_result.get("alternative", [])) == 0:
        raise UnknownValueError("音声データを認識できませんでした")

    best:str | None = None
    for rst in actual_result:
        if not rst["alternative"] is None and 0 < len(rst["alternative"]):
            for alt in rst["alternative"]: #問答無用に一番上でよい気がする
                if not alt["transcript"] is None:
                    best = alt["transcript"]
                    break
        if not best is None:
            break

    if best is None:
        raise UnknownValueError("レスポンスにtranscriptが存在しません", response_text)
    else:
        return RecognizeResult(best, response_text)


class UnknownValueError(exception.IlluminateException):
    """
    音声認識APIが音声を認識できなかった場合投げられる
    """
    def __init__(self, message: str, raw_data:str | None = None, inner: Exception | None = None):
        super().__init__(message, inner)
        self._raw_data = raw_data

    @property
    def raw_data(self) -> str | None:
        """
        Noneでない場合APIの戻り値生データが入る
        """
        return self._raw_data
    
class HttpStatusError(exception.IlluminateException):
    def __init__(self, message:str, status_code:int, inner:Exception | None = None):
        super().__init__(message, inner)
        self.__status_code = status_code

    @property
    def status_code(self) -> int:
        return self.__status_code
