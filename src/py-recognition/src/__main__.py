#!/usr/bin/env python3

import os
import sys
import traceback
import click
import torch
import speech_recognition as sr
import urllib.error as urlerr
import numpy as np
import datetime as dt
from concurrent.futures import ThreadPoolExecutor
from typing import cast, Optional

import src.mic as mic_
import src.recognition as recognition
import src.output as output
import src.val as val
import src.google_recognizers as google
from src.env import Env
from src.cancellation import CancellationObject
from src.filter import *


@click.command()
@click.option("--test", default="", help="テストを行います",type=click.Choice(["", val.TEST_VALUE_RECOGNITION, val.TEST_VALUE_MIC]))
@click.option("--method", default=val.METHOD_VALUE_WHISPER_FASTER, help="使用する認識方法", type=click.Choice([val.METHOD_VALUE_WHISPER, val.METHOD_VALUE_WHISPER_FASTER, val.METHOD_VALUE_GOOGLE, val.METHOD_VALUE_GOOGLE_DUPLEX]))
@click.option("--whisper_model", default="medium", help="(whisper)使用する推論モデル", type=click.Choice(["tiny","base", "small","medium","large","large-v2","large-v3"]))
@click.option("--whisper_device", default=("cuda" if torch.cuda.is_available() else "cpu"), help="(whisper)使用する演算装置", type=click.Choice(["cpu","cuda"]))
@click.option("--whisper_language", default="", help="(whisper)音声解析対象の言語", type=click.Choice(val.LANGUAGE_CODES))
@click.option("--google_language", default="ja-JP", help="(google)音声解析対象の言語", type=str)
@click.option("--google_timeout", default=5.0, help="(google)最大認識待ち時間", type=float)
@click.option("--google_convert_sampling_rate", default=False, help="(google)マイク入力を16kに変換します", is_flag=True, type=bool)
@click.option("--google_error_retry", default=1, help="(google)500エラー時にリトライ試行する回数", type=int)
@click.option("--mic", default=None, help="使用するマイクのindex", type=int)
@click.option("--mic_energy", default=300, help="設定した値より小さいマイク音量を無音として扱います", type=float)
@click.option("--mic_dynamic_energy", default=False,is_flag=True, help="Trueの場合周りの騒音に基づいてマイクのエネルギーレベルを動的に変更します", type=bool)
@click.option("--mic_pause", default=0.8, help="無音として認識される秒数を指定します", type=float)
@click.option("--mic_sampling_rate", default=16000, help="-", type=int)
@click.option("--out", default=val.OUT_VALUE_PRINT, help="認識結果の出力先", type=click.Choice([val.OUT_VALUE_PRINT,val.OUT_VALUE_YUKARINETTE, val.OUT_VALUE_YUKACONE]))
@click.option("--out_yukarinette",default=49513, help="ゆかりねっとの外部連携ポートを指定", type=int)
@click.option("--out_yukacone",default=None, help="ゆかコネNEOの外部連携ポートを指定", type=int)
#@click.option("--out_illuminate",default=495134, help="未実装",type=int)
@click.option("--filter_lpf_cutoff", default=200, help="ローパスフィルタのカットオフ周波数を設定", type=int)
@click.option("--filter_lpf_cutoff_upper", default=200, help="ローパスフィルタのカットオフ周波数(アッパー)を設定", type=int)
@click.option("--filter_hpf_cutoff", default=200, help="ハイパスフィルタのカットオフ周波数を設定します", type=int)
@click.option("--filter_hpf_cutoff_upper", default=200, help="ハイパスフィルタのカットオフ周波数(アッパー)を設定", type=int)
@click.option("--disable_lpf", default=False, help="ローパスフィルタを使用しません", is_flag=True, type=bool)
@click.option("--disable_hpf", default=False, help="ハイパスフィルタを使用しません", is_flag=True, type=bool)
@click.option("--print_mics",default=False, help="マイクデバイスの一覧をプリント", is_flag=True, type=bool)
@click.option("--list_devices",default=False, help="(廃止予定)--print_micsと同じ", is_flag=True, type=bool)
@click.option("--verbose", default="0", help="出力ログレベルを指定", type=click.Choice(["0", "1", "2"]))
def main(
    test:str,
    method:str,
    whisper_model:str,
    whisper_device:str,
    whisper_language:str,
    google_language:str,
    google_timeout:float,
    google_convert_sampling_rate:bool,
    google_error_retry:int,
    mic:Optional[int],
    mic_energy:float,
    mic_dynamic_energy:bool,
    mic_pause:float,
    mic_sampling_rate:int,
    out:str,
    out_yukarinette:int,
    out_yukacone:Optional[int],
#    out_illuminate:int,
    filter_lpf_cutoff:int,
    filter_lpf_cutoff_upper:int,
    filter_hpf_cutoff:int,
    filter_hpf_cutoff_upper:int,
    disable_lpf:bool,
    disable_hpf:bool,
    print_mics:bool,
    list_devices:bool,
    verbose:str) -> None:


    env = Env(int(verbose))
    if not env.is_exe:
        os.makedirs(env.root, exist_ok=True)
        os.chdir(env.root)

    if print_mics or list_devices:
        audio = sr.Microphone.get_pyaudio().PyAudio()
        try:
            for i in range(audio.get_device_count()):
                device_info = audio.get_device_info_by_index(i)
                index = device_info.get("index")
                host_api = device_info.get("hostApi")
                name = device_info.get("name")
                input = device_info.get("maxInputChannels")
                host_api_name = "-"
                rate = device_info.get("defaultSampleRate")
                if isinstance(host_api, int):
                    host_api_name = audio.get_host_api_info_by_index(host_api).get("name")
                if isinstance(input, int) and 0 < input:
                    print(f"{index} : [{host_api_name}]{name} sample_rate={rate}")            
        finally:
            audio.terminate()
        return

    cancel = CancellationObject()
    thread_pool = ThreadPoolExecutor(max_workers=1)
    try:
        sampling_rate = mic_.Mic.update_sample_rate(mic, mic_sampling_rate) #16000

        if test == val.TEST_VALUE_MIC:
            mic_.Mic(
                sampling_rate,
                mic_energy,
                mic_pause,
                mic_dynamic_energy,
                mic).test_mic(cancel)
            return

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
                challenge=google_error_retry),
        }[method]()
        env.tarce(lambda: print(f"#認識モデルは{type(recognition_model)}を使用"))

        print("マイクの初期化")
        mc = mic_.Mic(
            sampling_rate,
            mic_energy,
            mic_pause,
            mic_dynamic_energy,
            mic)
        print(f"マイクは{mc.device_name}を使用します")

        outputer:output.RecognitionOutputer = {
            val.OUT_VALUE_PRINT: lambda: output.PrintOutputer(),
            val.OUT_VALUE_YUKARINETTE: lambda: output.YukarinetteOutputer(f"ws://localhost:{out_yukarinette}"),
            val.OUT_VALUE_YUKACONE: lambda: output.YukaconeOutputer(f"ws://localhost:{output.YukaconeOutputer.get_port(out_yukacone)}"),
#            val.OUT_VALUE_ILLUMINATE: lambda: output.IlluminateSpeechOutputer(f"ws://localhost:{out_illuminate}"),
        }[out]()
        env.tarce(lambda: print(f"#出力は{type(outputer)}を使用"))

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
        env.tarce(lambda: print(f"#使用音声フィルタ({len(filters)}):"))
        for f in filters:
            env.tarce(lambda: print(f"#{type(f)}"))

        def onrecord(data:bytes) -> None:
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

            env.tarce(lambda: print(f"#録音データ取得(time={dt.datetime.now()}, pcm={(int)(len(data)/2)},{round(len(data)/sampling_rate, 2)}s)"))
            try:
                if recognition_model.required_sample_rate is None:
                    d = data
                else:
                    d = sr.AudioData(data, sampling_rate, 2).get_wav_data(recognition_model.required_sample_rate, 2)

                r = env.performance(lambda: recognition_model.transcribe(filter(np.frombuffer(d, np.int16).flatten())))
                if r.result[0] not in ["", " ", "\n", None]:
                    env.debug(lambda: print(f"認識時間[{r.time}ms]", end=": "))
                    outputer.output(r.result[0])
                if not r[1] is None:
                    env.tarce(lambda: print(f"{r.result[1]}"))
            except recognition.TranscribeException as e:
                if e.inner is None:
                    print(e.message)
                else:
                    if isinstance(e.inner, urlerr.HTTPError) or isinstance(e.inner, urlerr.URLError):
                        env.debug(lambda: print(e.message))
                    elif isinstance(e.inner, google.UnknownValueError):
                        er = cast(google.UnknownValueError, e.inner)
                        if er.raw_data is None:
                            env.tarce(lambda: print(f"#{e.message}"))
                        else:
                            env.tarce(lambda: print(f"#{e.message}\r\n{er.raw_data}"))
                    else:
                        env.tarce(lambda: print(f"#{e.message}"))
                        env.tarce(lambda: print(f"#{type(e.inner)}:{e.inner}"))
            except output.WsOutputException as e:
                print(e.message)
                if not e.inner is None:
                    env.tarce(lambda: print(f"# => {type(e.inner)}:{e.inner}"))
            except Exception as e:
                print(f"!!!!意図しない例外({type(e)}:{e})!!!!")
                print(traceback.format_exc())
            env.tarce(lambda: print(f"#認識処理終了(time={dt.datetime.now()})"))

        def onrecord_async(data:bytes) -> None:
            thread_pool.submit(onrecord, data)


        print("認識中…")
        if test == val.TEST_VALUE_RECOGNITION:
            mc.listen(onrecord)
            return

        mc.listen_loop(onrecord_async, cancel)
    except mic_.MicInitializeExeception as e:
        print(e.message)
        print(f"{type(e.inner)}{e.inner}")
    except KeyboardInterrupt:
        cancel.cancel()
        print("ctrl+c")
    sys.exit()


if __name__ == "__main__":
    main() # type: ignore
