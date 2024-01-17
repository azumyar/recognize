import torch
import whisper
import faster_whisper as fwis
import numpy as np
import speech_recognition as sr
import urllib.error as urlerr
import requests.exceptions
from typing import Any, NamedTuple, Callable

import src.exception as ex
import src.google_recognizers as google


class TranscribeResult(NamedTuple):
    """
    RecognitionModel#transcribeの戻り値データ型
    """
    transcribe:str
    extend_data:Any

class RecognitionModel:
    """
    認識モデル抽象基底クラス
    """

    @property
    def required_sample_rate(self) -> int | None:
        ...

    def transcribe(self, _:np.ndarray) -> TranscribeResult:
        ...

class RecognitionModelWhisper(RecognitionModel):
    """
    認識モデルのwhisper実装
    """
    def __init__(
        self,
        model:str,
        language:str,
        device:str,
        download_root:str) -> None:
        self.__is_fp16 = device == "cuda"
        self.__language = language if language != "" else None

        m = f"{model}.{language}" if (model != "large") and (model != "large-v2") and (model != "large-v3") and (language == "en") else model
        self.audio_model = whisper.load_model(m, download_root=download_root).to(device)

    @property
    def required_sample_rate(self) -> int | None:
        return 16000

    def transcribe(self, audio_data:np.ndarray) -> TranscribeResult:
        r = self.audio_model.transcribe(
            torch.from_numpy(audio_data.astype(np.float32) / float(np.iinfo(np.int16).max)),
            language = self.__language,
            fp16 = self.__is_fp16)["text"]
        if isinstance(r, str):
            return TranscribeResult(r, None)
        if isinstance(r, list):
            return TranscribeResult("".join(r), None)
        raise ex.ProgramError(f"Whisper.transcribeから意図しない戻り値型:{type(r)}")

class RecognitionModelWhisperFaster(RecognitionModel):
    """
    認識モデルのfaster_whisper実装
    """
    def __init__(
        self,
        model:str,
        language:str,
        device:str,
        download_root:str) -> None:
        self.__language = language if language != "" else None

        def get(device:str) -> tuple[str, str]:
            if device == "cuda":
                try:
                    import torch
                    if torch.cuda.is_available():
                        mj, mi = torch.cuda.get_device_capability()
                        if 7 <= mj:
                            return ("cuda", "float16")
                        elif mj == 6 and 1 <= mi:
                            return ("cuda", "int8")
                        else:
                            return ("cpu", "int8")
                except:
                    pass
            return ("cpu", "int8")

        m = f"{model}.{language}" if (model != "large") and (model != "large-v2") and (language == "en") else model
        run_device, compute_type = get(device)
        self.audio_model = fwis.WhisperModel(
            m,
            run_device,
            compute_type = compute_type,
            download_root=download_root)

    @property
    def required_sample_rate(self) -> int | None:
        return 16000

    def transcribe(self, audio_data:np.ndarray) -> TranscribeResult:
        segments, _  = self.audio_model.transcribe(
            audio_data.astype(np.float32) / float(np.iinfo(np.int16).max),
            language = self.__language)
        c = []
        for s in segments:
            c.append(s.text)
        return TranscribeResult("".join(c), segments)

class RecognitionModelGoogleApi(RecognitionModel):
    """
    google系認識モデルの基底クラス    
    """
    def __init__(
        self,
        api:Callable[..., google.RecognizeResult],
        sample_rate:int,
        sample_width:int,
        convert_sample_rete:bool=False,
        language:str="ja-JP",
        key:str | None=None,
        timeout:float | None = None,
        challenge:int = 1):
        self.__api = api
        self.__sample_rate = sample_rate
        self.__sample_width = sample_width
        self.__language = language
        self.__key = key
        self.__operation_timeout = timeout
        self.__max_loop = challenge
        self.__convert_sample_rete = convert_sample_rete

    @property
    def required_sample_rate(self) -> int | None:
        return None

    def transcribe(self, audio_data:np.ndarray) -> TranscribeResult:
        flac = google.encode_falc(
            sr.AudioData(audio_data.astype(np.int16, order="C"), self.__sample_rate, self.__sample_width),
            None if not self.__convert_sample_rete else 16000)

        loop = 0
        while loop < self.__max_loop:
            try:
                r = self.__api(
                    flac,
                    self.__operation_timeout,
                    language=self.__language,
                    key = self.__key)
                return TranscribeResult(r.transcript, r.raw_data)
            except urlerr.HTTPError as e:
                raise TranscribeException("google音声認識でHTTPエラー: {}".format(e.reason), e)
            except urlerr.URLError as e:
                raise TranscribeException("google音声認識でリモート接続エラー: {}".format(e.reason), e)
            except google.HttpStatusError as e:
                raise TranscribeException("google音声認識でHTTPエラー: {}".format(e.message), e)
            except requests.exceptions.ConnectionError as e:
                raise TranscribeException("google音声認識でリモート接続エラー: {}".format(e), e)

            except google.UnknownValueError as e:
                raise TranscribeException(
                    f"googleは音声データを検出できませんでした",
                    e)

            except requests.exceptions.ReadTimeout:
                if self.__max_loop == 1:
                    raise TranscribeException(f"google音声認識でリモート接続がタイムアウトしました")
            except TimeoutError:
                if self.__max_loop == 1:
                    raise TranscribeException(f"google音声認識でリモート接続がタイムアウトしました")
            loop += 1
        raise TranscribeException(f"{self.__max_loop}回試行しましたが失敗しました")
 
class RecognitionModelGoogle(RecognitionModelGoogleApi):
    """
    認識モデルのgoogle音声認識API v2実装
    """
    def __init__(self, sample_rate: int, sample_width: int, convert_sample_rete: bool=False, language: str = "ja-JP", key: str | None = None, timeout: float | None = None, challenge: int = 1):
        super().__init__(google.recognize_google, sample_rate, sample_width, convert_sample_rete, language, key, timeout, challenge)


class RecognitionModelGoogleDuplex(RecognitionModelGoogleApi):
    """
    認識モデルのgoogle全二重API実装
    """
    def __init__(self, sample_rate: int, sample_width: int, convert_sample_rete: bool=False, language: str = "ja-JP", key: str | None = None, timeout: float | None = None, challenge: int = 1):
        super().__init__(google.recognize_google_duplex, sample_rate, sample_width, convert_sample_rete, language, key, timeout, challenge)


class TranscribeException(ex.IlluminateException):
    """
    認識に失敗した際なげる例外
    """
    pass
