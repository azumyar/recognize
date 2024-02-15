import os
import datetime as dt
import numpy as np
import audioop
import urllib.error as urlerr
from ctypes import cast
import traceback

import src.mic as mic_
import src.recognition as recognition
import src.output as output
import src.val as val
import src.google_recognizers as google
from src.env import Env
from src.cancellation import CancellationObject
from src.filter import *
from src.interop import print


def main_feature_mp(
    q,
    cancel,
    sampling_rate,
    method,
    whisper_model,
    whisper_language,
    whisper_device,
    google_convert_sampling_rate,
    google_language,
    google_timeout,
    google_error_retry,
    google_duplex_parallel,
    out,
    out_yukarinette,
    out_yukacone,
    disable_lpf,
    filter_lpf_cutoff,
    filter_lpf_cutoff_upper,
    disable_hpf,
    filter_hpf_cutoff,
    filter_hpf_cutoff_upper,
    record,
    logger,
    verbose,
    _:str) -> None:

    env = Env(int(verbose))
    print("認識モデルの初期化")
    recognition_model:recognition.RecognitionModel = {
        val.METHOD_VALUE_WHISPER: lambda: recognition.RecognitionModelWhisper(
            model=whisper_model,
            language=whisper_language,
            device=whisper_device,
            download_root=f"{env.root}{os.sep}.cache"),
        val.METHOD_VALUE_WHISPER_FASTER: lambda:  recognition.RecognitionModelWhisperFaster(
            model=whisper_model,
            language=whisper_language,
            device=whisper_device,
            download_root=f"{env.root}{os.sep}.cache"),
        val.METHOD_VALUE_GOOGLE: lambda: recognition.RecognitionModelGoogle(
            sample_rate=sampling_rate,
            sample_width=2,
            convert_sample_rete=google_convert_sampling_rate,
            language=google_language,
            timeout=google_timeout if 0 < google_timeout else None,
            challenge=google_error_retry),
        val.METHOD_VALUE_GOOGLE_DUPLEX: lambda: recognition.RecognitionModelGoogleDuplex(
            sample_rate=sampling_rate,
            sample_width=2,
            convert_sample_rete=google_convert_sampling_rate,
            language=google_language,
            timeout=google_timeout if 0 < google_timeout else None,
            challenge=google_error_retry,
            is_parallel_run=google_duplex_parallel),
    }[method]()
    logger.debug(f"#認識モデルは{type(recognition_model)}を使用")

    outputer:output.RecognitionOutputer = {
        val.OUT_VALUE_PRINT: lambda: output.PrintOutputer(),
        val.OUT_VALUE_YUKARINETTE: lambda: output.YukarinetteOutputer(f"ws://localhost:{out_yukarinette}", lambda x: logger.print(x)),
        val.OUT_VALUE_YUKACONE: lambda: output.YukaconeOutputer(f"ws://localhost:{output.YukaconeOutputer.get_port(out_yukacone)}", lambda x: logger.print(x)),
#            val.OUT_VALUE_ILLUMINATE: lambda: output.IlluminateSpeechOutputer(f"ws://localhost:{out_illuminate}"),
    }[out]()
    logger.debug(lambda: print(f"#出力は{type(outputer)}を使用"))

    filters:list[NoiseFilter] = []
    if not disable_lpf:
        filters.append(
            LowPassFilter(
                sampling_rate,
                filter_lpf_cutoff,
                filter_lpf_cutoff_upper))
    if not disable_hpf:
        filters.append(
            HighPassFilter(
                sampling_rate,
                filter_hpf_cutoff,
                filter_hpf_cutoff_upper))        
    logger.debug(f"#使用音声フィルタ({len(filters)}):")
    for f in filters:
        logger.debug(f"#{type(f)}")

    def onrecord(index:int, data:bytes) -> None:
        """
        マイク認識データが返るコールバック関数
        """
        def filter(ary:np.ndarray) -> np.ndarray:
            """
            フィルタ処理をする
            """
            if len(filters) == 0:
                return ary
            else:
                fft = np.fft.fft(ary)
                for f in filters:
                    f.filter(fft)
                return np.real(np.fft.ifft(fft))

        pcm_sec = len(data) / 2 / sampling_rate
        logger.debug(lambda: print(f"#録音データ取得(#{index}, time={dt.datetime.now()}, pcm={(int)(len(data)/2)},{round(pcm_sec, 2)}s)"))
        try:
            #save_wav(record, index, data, sampling_rate)
            if recognition_model.required_sample_rate is None or sampling_rate == recognition_model.required_sample_rate:
                d = data
            else:
                d, _ = audioop.ratecv(
                    data,
                    2, # sample_width
                    1,
                    sampling_rate,
                    recognition_model.required_sample_rate,
                    None)

            r = env.performance(lambda: recognition_model.transcribe(filter(np.frombuffer(d, np.int16).flatten())))
            if r.result[0] not in ["", " ", "\n", None]:
                logger.info(f"認識時間[{r.time}ms],PCM[{round(pcm_sec, 2)}s],{round(r.time/1000.0/pcm_sec, 2)}tps", end=": ")
                outputer.output(r.result[0])
            if not r[1] is None:
                logger.debug(f"{r.result[1]}")
        except recognition.TranscribeException as e:
            if e.inner is None:
                logger.print(e.message)
            else:
                if isinstance(e.inner, urlerr.HTTPError) or isinstance(e.inner, urlerr.URLError):
                    logger.info(lambda: print(e.message))
                elif isinstance(e.inner, google.UnknownValueError):
                    raw = e.inner.raw_data
                    if raw is None:
                        logger.debug(f"#{e.message}")
                    else:
                        logger.debug(f"#{e.message}\r\n{raw}")
                else:
                    logger.debug(f"#{e.message}")
                    logger.debug(f"#{type(e.inner)}:{e.inner}")
        except output.WsOutputException as e:
            logger.print(e.message)
            if not e.inner is None:
                logger.debug(f"# => {type(e.inner)}:{e.inner}")
        except Exception as e:
            logger.print(f"!!!!意図しない例外({type(e)}:{e})!!!!")
            logger.print(traceback.format_exc())
        logger.debug(lambda: print(f"#認識処理終了(#{index}, time={dt.datetime.now()})"))

    import time
    print("認識中…")
    index = 1
    while cancel.value != 0:
        if not q.empty():
            data = q.get()
            onrecord(index, data)
            index += 1
        time.sleep(0.01)