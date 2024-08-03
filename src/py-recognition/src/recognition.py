import os
import time
import math
import numpy as np
import speech_recognition as sr
import urllib.error as urlerr
import requests.exceptions
import concurrent.futures
from typing import Any, NamedTuple, Callable

import src.exception as ex
import src.google_recognizers as google


class TranscribeResult(NamedTuple):
    """
    RecognitionModel#transcribeの戻り値データ型
    """
    transcribe:str
    extend_data:Any

class GoogleTranscribeExtend(NamedTuple):
    """
    RecognitionModel#transcribeの戻り値データ型
    """
    raw_data:Any
    retry_history:list[Exception]

    def __str__(self) -> str:
        if 0 < len(self.retry_history):
            clazz = os.linesep.join([f"{type(i)}:{i}"for i in self.retry_history])
            return f"{self.raw_data}{os.linesep}retry-stack{os.linesep}{clazz}"
        else:
            return str(self.raw_data)

class RecognizeMicrophoneConfig:
    def __init__(self, head_insert_duration:float, tail_insert_duration:float) -> None:
        self.__head_insert_duration = head_insert_duration
        self.__tail_insert_duration = tail_insert_duration

    @property
    def head_insert_duration(self) -> float:
        return self.__head_insert_duration

    @property
    def tail_insert_duration(self) -> float:
        return self.__tail_insert_duration


class WhisperMicrophoneConfig(RecognizeMicrophoneConfig):
    __DEFAULT_HEAD_DULATION = 0.
    __DEFAULT_TAIL_DULATION = 0.

    def __init__(self, head_insert_duration:float | None = None, tail_insert_duration:float | None = None) -> None:
        super().__init__(
            head_insert_duration if not head_insert_duration is None else WhisperMicrophoneConfig.__DEFAULT_HEAD_DULATION,
            tail_insert_duration if not tail_insert_duration is None else WhisperMicrophoneConfig.__DEFAULT_TAIL_DULATION)

class GoogleMicrophoneConfig(RecognizeMicrophoneConfig):
    __DEFAULT_HEAD_DULATION = 0.2
    __DEFAULT_TAIL_DULATION = 0.25

    def __init__(self, head_insert_duration:float | None = None, tail_insert_duration:float | None = None) -> None:
        super().__init__(
            head_insert_duration if not head_insert_duration is None else GoogleMicrophoneConfig.__DEFAULT_HEAD_DULATION,
            tail_insert_duration if not tail_insert_duration is None else GoogleMicrophoneConfig.__DEFAULT_TAIL_DULATION)


class RecognitionModel:
    """
    認識モデル抽象基底クラス
    """

    @property
    def required_sample_rate(self) -> int | None:
        ...

    def transcribe(self, _:np.ndarray) -> TranscribeResult:
        ...

    def get_verbose(self, _:int) -> str | None:
        ...

    def get_log_info(self) -> str:
        ...

try:
    import whisper # type: ignore
    import torch # type: ignore
except:
    pass
else:
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

        def get_verbose(self, _:int) -> str | None:
            return None

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

        def get_log_info(self) -> str:
            return ""

try:
    import faster_whisper # type: ignore
    import torch # type: ignore
except:
    pass
else:
    class RecognitionModelWhisperFaster(RecognitionModel):
        """
        認識モデルのfaster_whisper実装
        """
        def __init__(
            self,
            model:str,
            language:str,
            device:str,
            device_index:int,
            download_root:str) -> None:
            self.__language = language if language != "" else None

            def get(device:str) -> tuple[str, str]:
                if device == "cuda":
                    try:
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
            self.audio_model = faster_whisper.WhisperModel(
                m,
                run_device,
                device_index = device_index,
                compute_type = compute_type,
                download_root = download_root)

        @property
        def required_sample_rate(self) -> int | None:
            return 16000

        def get_verbose(self, _:int) -> str | None:
            return None

        def get_log_info(self) -> str:
            return ""

        def transcribe(self, audio_data:np.ndarray) -> TranscribeResult:
            segments, _  = self.audio_model.transcribe(
                audio_data.astype(np.float32) / float(np.iinfo(np.int16).max),
                language = self.__language,
                beam_size=5)
                #max_new_tokens = 128,
                #condition_on_previous_text = False)
            c = []
            for s in segments:
                c.append(s.text)
            return TranscribeResult("".join(c), segments)


try:
    from transformers import pipeline # type: ignore
    import torch # type: ignore
except:
    pass
else:
    class RecognitionModelWhisperKotoba(RecognitionModel):
        def __init__(self, device:str) -> None:
            model_id = "kotoba-tech/kotoba-whisper-v1.0"
            torch_dtype = torch.bfloat16 if torch.cuda.is_available() else torch.float32
            model_kwargs = {"attn_implementation": "sdpa"} if torch.cuda.is_available() else {}

            self.__generate_kwargs = {"language": "japanese", "task": "transcribe"}
            self.__pipe = pipeline(
                "automatic-speech-recognition",
                model = model_id,
                torch_dtype = torch_dtype,
                device = device,
                model_kwargs = model_kwargs,
                chunk_length_s=15,
                batch_size=16
            )

        @property
        def required_sample_rate(self) -> int | None:
            return 16000

        def get_verbose(self, _:int) -> str | None:
            return None

        def transcribe(self, audio_data:np.ndarray) -> TranscribeResult:
            reslut = self.__pipe(
                audio_data.astype(np.float32) / float(np.iinfo(np.int16).max),
                generate_kwargs = self.__generate_kwargs)
            r = reslut["text"] #type: ignore
            if isinstance(r, str):
                return TranscribeResult(r, None)
            if isinstance(r, list):
                return TranscribeResult("".join(r), None)
            raise ex.ProgramError(f"pipelineから意図しない戻り値型:{type(r)}")


class RecognitionModelGoogleApi(RecognitionModel):
    """
    google系認識モデルの基底クラス    
    """
    def __init__(
        self,
        sample_rate:int,
        sample_width:int,
        convert_sample_rete:bool=False,
        language:str="ja-JP",
        key:str | None=None,
        timeout:float | None = None,
        challenge:int = 1):
        self.__sample_rate = sample_rate
        self.__sample_width = sample_width
        self.__max_loop = max(1, challenge)
        self.__convert_sample_rete = convert_sample_rete
        self._language = language
        self._key = key
        self._operation_timeout = timeout

    @property
    def required_sample_rate(self) -> int | None:
        return None

    def get_verbose(self, _:int) -> str | None:
        return None

    def get_log_info(self) -> str:
        return ""

    def transcribe(self, audio_data:np.ndarray) -> TranscribeResult:
        flac = google.encode_falc(
            sr.AudioData(audio_data.astype(np.int16, order="C"), self.__sample_rate, self.__sample_width),
            None if not self.__convert_sample_rete else 16000)

        his = []
        loop = 0
        while loop < self.__max_loop:
            try:
                r = self._transcribe_impl(flac)
                return TranscribeResult(r.transcribe, GoogleTranscribeExtend(r.extend_data, his))
            except urlerr.HTTPError as e:
                if (e.code == 500) and (1 < self.__max_loop):
                    his.append(e)
                else:
                    raise TranscribeException("google音声認識でHTTPエラー: {}".format(e.reason), e)
            except urlerr.URLError as e:
                raise TranscribeException("google音声認識でリモート接続エラー: {}".format(e.reason), e)
            except google.HttpStatusError as e:
                if (e.status_code == 500) and (1 < self.__max_loop):
                    his.append(e)
                else:
                    raise TranscribeException("google音声認識でHTTPエラー: {}".format(e.message), e)
            except requests.exceptions.ConnectionError as e:
                raise TranscribeException("google音声認識でリモート接続エラー: {}".format(e), e)

            except google.UnknownValueError as e:
                raise TranscribeException(
                    f"googleは音声データを検出できませんでした",
                    e)

            except requests.exceptions.ReadTimeout as e:
                raise TranscribeException(f"google音声認識でリモート接続がタイムアウトしました")
                #if self.__max_loop == 1:
                #    raise TranscribeException(f"google音声認識でリモート接続がタイムアウトしました")
                #else:
                #    his.append(e)
            except TimeoutError as e:
                raise TranscribeException(f"google音声認識でリモート接続がタイムアウトしました")
                #if self.__max_loop == 1:
                #    raise TranscribeException(f"google音声認識でリモート接続がタイムアウトしました")
                #else:
                #    his.append(e)
            except ParallelTranscribeException as e:
                if (e.is_error500) and (1 < self.__max_loop):
                    his.append(e)
                else:
                    raise TranscribeException("google音声認識でHTTPエラー: {}".format(e), e)
            loop += 1
        clazz = ",".join([f"{type(i)}"for i in his])
        raise TranscribeException(f"{self.__max_loop}回試行しましたが失敗しました({clazz}])")
 
    def _transcribe_impl(self, flac:google.EncodeData) -> TranscribeResult:
        ...

class RecognitionModelGoogle(RecognitionModelGoogleApi):
    """
    認識モデルのgoogle音声認識API v2実装
    """
    def __init__(self, sample_rate: int, sample_width: int, convert_sample_rete: bool=False, language: str = "ja-JP", key: str | None = None, timeout: float | None = None, challenge: int = 1):
        super().__init__(sample_rate, sample_width, convert_sample_rete, language, key, timeout, challenge)


    def _transcribe_impl(self, flac:google.EncodeData) -> TranscribeResult:
        r = google.recognize_google(
            flac,
            self._operation_timeout,
            self._key,
            self._language,
            0)
        return TranscribeResult(r.transcript, r.raw_data)


class RecognitionModelGoogleDuplex(RecognitionModelGoogleApi):
    """
    認識モデルのgoogle全二重API実装
    """

    __MAX_PARALLEL_SUCESS = 3
    '''スレッド数を下げるために必要な平行実行の成功回数'''
    __MIN_PARALLEL = 3
    '''変動スレッド数の最低値'''
    __MAX_PARALLEL = 6
    '''変動スレッド数の最大値'''

    def __init__(
        self,
        sample_rate:int,
        sample_width:int,
        convert_sample_rete:bool=False,
        language:str = "ja-JP",
        key:str|None = None,
        timeout:float|None = None,
        challenge:int = 1,
        is_parallel_run:bool = False,
        parallel_max:int|None = None,
        parallel_reduce_count:int|None = None):

        super().__init__(sample_rate, sample_width, convert_sample_rete, language, key, timeout, challenge)
        self.__is_parallel_run = is_parallel_run
        self.__parallel = 3
        '''平行実行のスレッド数'''
        self.__parallel_successed = 0
        '''平行実行の成功回数'''
        self.__parallel_max = RecognitionModelGoogleDuplex.__MAX_PARALLEL
        '''平行実行の最大数'''
        if not parallel_max is None:
            self.__parallel_max = max(parallel_max, RecognitionModelGoogleDuplex.__MIN_PARALLEL)
        self.__parallel_reduce_count = RecognitionModelGoogleDuplex.__MAX_PARALLEL_SUCESS
        '''平行実行を減少させるための必要な成功数'''
        if not parallel_reduce_count is None:
            self.__parallel_reduce_count = max(parallel_reduce_count, 1)

    def get_verbose(self, verbose:int) -> str | None:
        if verbose < 2:
            return None
        if not self.__is_parallel_run:
            return None
        return f"current parallel num = {self.__parallel}"

    def get_log_info(self) -> str | None:
        return f"current parallel num = {self.__parallel}"

    def _transcribe_impl(self, flac:google.EncodeData) -> TranscribeResult:
        class Extend(NamedTuple):
            exceptions:list[Exception]
            raw_data:str

            def __str__(self) -> str:
                if 0 < len(self.exceptions):
                    return f"{self.raw_data}{os.linesep}{len(self.exceptions)}回の失敗:{os.linesep}{f'{os.linesep}'.join(map(lambda x: f'{type(x)}:{x}', self.exceptions))}"
                else:
                    return f"{self.raw_data}"
        def func(index:int = 0, delay_ratio=0.1) -> TranscribeResult:
            if RecognitionModelGoogleDuplex.__MIN_PARALLEL < index:
                # 増加スレッドは遅延させてから実行する
                wait = math.ceil(index / RecognitionModelGoogleDuplex.__MIN_PARALLEL) - 1
                if(0 < wait):
                    time.sleep(wait * delay_ratio)
            r = google.recognize_google_duplex(
                flac,
                self._operation_timeout,
                self._key,
                self._language,
                0)
            return TranscribeResult(r.transcript, r.raw_data)

        if self.__is_parallel_run:
            thread_pool = concurrent.futures.ThreadPoolExecutor(max_workers=self.__parallel)
            try:
                futures = [thread_pool.submit(func, i) for i in range(self.__parallel)]
                ex:list[Exception] = []
                for future in concurrent.futures.as_completed(futures):
                    try:
                        r = future.result()
                        self.__parallel_successed += 1
                        if(self.__parallel_reduce_count < self.__parallel_successed):
                            self.__parallel_successed = 0
                            self.__parallel = max(self.__parallel - 1, RecognitionModelGoogleDuplex.__MIN_PARALLEL)
                        return TranscribeResult(r.transcribe, Extend(ex, f"{r.extend_data}"))
                    except Exception as e:
                        ex.append(e)

                raise_ex = ParallelTranscribeException("すべての並列実行が失敗", ex)
                if raise_ex.is_error500:
                    self.__parallel_successed = 0
                    self.__parallel = min(self.__parallel + 1, self.__parallel_max)
                raise raise_ex
            finally:
                thread_pool.shutdown(wait=False)
        else:
            return func()

class RecognitionModelGoogleMix(RecognitionModelGoogleApi):
    """
    認識モデルのgoogle全二重API実装
    """

    __MAX_PARALLEL_SUCESS = 3
    '''スレッド数を下げるために必要な平行実行の成功回数(duplex)'''
    __MIN_PARALLEL_DUPLEX = 3
    '''変動スレッド数の最低値(duplex)'''
    __MAX_PARALLEL_DUPLEX = 6
    '''変動スレッド数の最大値(duplex)'''

    API_RECOGNIZE = 1
    '''API-No:recognize API'''
    API_DUPLEX = 2
    '''API-No:duplex API'''


    def __init__(
        self,
        sample_rate:int,
        sample_width:int,
        convert_sample_rete:bool=False,
        language:str = "ja-JP",
        key:str|None = None,
        timeout:float|None = None,
        challenge:int = 1,
        parallel_max_duplex:int|None = None,
        parallel_reduce_count_duplex:int|None = None):

        super().__init__(sample_rate, sample_width, convert_sample_rete, language, key, timeout, challenge)
        self.__parallel_recognize = 1
        '''平行実行のスレッド数(recognize)'''


        self.__parallel__duplex = 3
        '''平行実行のスレッド数(duplex)'''
        self.__parallel_successed__duplex = 0
        '''平行実行の成功回数(duplex)'''
        self.__parallel_max__duplex = RecognitionModelGoogleMix.__MAX_PARALLEL_DUPLEX
        '''平行実行の最大数(duplex)'''
        if not parallel_max_duplex is None:
            self.__parallel_max__duplex = max(parallel_max_duplex, RecognitionModelGoogleMix.__MIN_PARALLEL_DUPLEX)
        self.__parallel_reduce_count__duplex = RecognitionModelGoogleMix.__MAX_PARALLEL_SUCESS
        '''平行実行を減少させるための必要な成功数(duplex)'''
        if not parallel_reduce_count_duplex is None:
            self.__parallel_reduce_count__duplex = max(parallel_reduce_count_duplex, 1)

    def get_verbose(self, verbose:int) -> str | None:
        if verbose < 2:
            return None
        return f"current parallel num = ({ self.__parallel_recognize}, {self.__parallel__duplex})"

    def get_log_info(self) -> str | None:
        return f"current parallel num = ({ self.__parallel_recognize}, {self.__parallel__duplex})"

    def _transcribe_impl(self, flac:google.EncodeData) -> TranscribeResult:
        class FutureResult(NamedTuple):
            api_no:int
            result:TranscribeResult

        class Extend(NamedTuple):
            exceptions:list[Exception]
            api_no:int
            raw_data:str

            def __str__(self) -> str:
                def api(i:int):
                    if i == RecognitionModelGoogleMix.API_RECOGNIZE:
                        return "recognize-api"
                    if i == RecognitionModelGoogleMix.API_DUPLEX:
                        return "duplex-api"
                    return "unknown-api"
                if 0 < len(self.exceptions):
                    return f"transcribe:{api(self.api_no)}{os.linesep}{self.raw_data}{os.linesep}{len(self.exceptions)}回の失敗:{os.linesep}{f'{os.linesep}'.join(map(lambda x: f'{type(x)}:{x}', self.exceptions))}"
                else:
                    return f"transcribe:{api(self.api_no)}{os.linesep}{self.raw_data}"

        def func_recognize(_:int = 0, __=0.1) -> FutureResult:
            r = google.recognize_google(
                flac,
                self._operation_timeout,
                self._key,
                self._language,
                0)
            return FutureResult(RecognitionModelGoogleMix.API_RECOGNIZE, TranscribeResult(r.transcript, r.raw_data))

        def func_duplex(index:int = 0, delay_ratio=0.1) -> FutureResult:
            if RecognitionModelGoogleMix.__MIN_PARALLEL_DUPLEX < index:
                # 増加スレッドは遅延させてから実行する
                wait = math.ceil(index / RecognitionModelGoogleMix.__MIN_PARALLEL_DUPLEX) - 1
                if(0 < wait):
                    time.sleep(wait * delay_ratio)
            r = google.recognize_google_duplex(
                flac,
                self._operation_timeout,
                self._key,
                self._language,
                0)
            return FutureResult(RecognitionModelGoogleMix.API_DUPLEX, TranscribeResult(r.transcript, r.raw_data))


        thread_pool = concurrent.futures.ThreadPoolExecutor(max_workers=self.__parallel__duplex + 2)
        try:
            futures = [thread_pool.submit(func_recognize) for _ in range( self.__parallel_recognize)] \
                + [thread_pool.submit(func_duplex, i) for i in range(self.__parallel__duplex)]
            ex:list[Exception] = []
            for future in concurrent.futures.as_completed(futures):
                try:
                    r = future.result()
                    if r.api_no == RecognitionModelGoogleMix.API_DUPLEX:
                        self.__parallel_successed__duplex += 1
                        if(self.__parallel_reduce_count__duplex < self.__parallel_successed__duplex):
                            self.__parallel_successed__duplex = 0
                            self.__parallel__duplex = max(self.__parallel__duplex - 1, RecognitionModelGoogleMix.__MIN_PARALLEL_DUPLEX)
                    return TranscribeResult(r.result.transcribe, Extend(ex, r.api_no, f"{r.result.extend_data}"))
                except Exception as e:
                    ex.append(e)

            raise_ex = ParallelTranscribeException("すべての並列実行が失敗", ex)
            if raise_ex.is_error500:
                self.__parallel_successed__duplex = 0
                self.__parallel = min(self.__parallel + 1, self.__parallel_max__duplex)
            raise raise_ex
        finally:
            thread_pool.shutdown(wait=False)

class TranscribeException(ex.IlluminateException):
    """
    認識に失敗した際なげる例外
    """
    pass

class ParallelTranscribeException(ex.IlluminateException):
    def __init__(self, message: str, exceptions:list[Exception]):
        super().__init__(message, None)
        self.__exceptions = exceptions

        self.__is_error500 = not (False in map(
            lambda x:isinstance(x, google.HttpStatusErrorDuplex)
              and x.status_code == 500,
            exceptions))

    def __str__(self) -> str:
        return f"並列実行がすべて失敗しました parallel={len(self.__exceptions)}, 500error={self.is_error500}, exception={','.join(list(map(lambda x: f'{type(x)}', self.__exceptions)))}"

    @property
    def is_error500(self) -> bool:
        return self.__is_error500
