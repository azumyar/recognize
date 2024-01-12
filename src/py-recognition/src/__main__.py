#!/usr/bin/env python3

import os
import sys
import click
import torch
import speech_recognition as sr
import urllib.error as urlerr
import numpy as np
from typing import cast, Optional
import datetime

from src.env import Env
from src.mic import Mic
from src.cancellation import CancellationObject
from src.filter import *
import src.transcribe as transcribe
import src.output as output
import src.val as val
import src.google_recognizers as google

@click.command()
@click.option("--test", default=False, help="一度だけ認識を行います", is_flag=True, type=bool)
@click.option("-m", "--method", default="faster_whisper", help="使用する認識方法", type=click.Choice(["whisper","faster_whisper", "google", "google_duplex"]))
@click.option("--whisper_model", default="base", help="(whisper)使用する推論モデル", type=click.Choice(["tiny","base", "small","medium","large","large-v2","large-v3"]))
@click.option("--whisper_device", default=("cuda" if torch.cuda.is_available() else "cpu"), help="(whisper)使用する演算装置", type=click.Choice(["cpu","cuda"]))
@click.option("--whisper_language", default="", help="(whisper)音声解析対象の言語", type=click.Choice(val.LANGUAGE_CODES))
@click.option("--google_language", default="ja-JP", help="(google)音声解析対象の言語", type=str)
@click.option("--google_timeout", default=5.0, help="(google)最大認識待ち時間", type=float)
@click.option("--mic", default=None, help="使用するマイクのindex", type=int)
@click.option("--mic_energy", default=300, help="設定した値より小さいマイク音量を無音として扱います", type=float)
@click.option("--mic_dynamic_energy", default=False,is_flag=True, help="Trueの場合周りの騒音に基づいてマイクのエネルギーレベルを動的に変更します", type=bool)
@click.option("--mic_pause", default=0.8, help="無音として認識される秒数を指定します", type=float)
@click.option("--list_devices",default=False, help="マイクデバイスのリストをプリント", is_flag=True, type=bool)
@click.option("-o", "--out", default="print", help="認識結果の出力先", type=click.Choice(["print","yukarinette", "yukacone"]))
@click.option("--out_yukarinette",default=49513, help="ゆかりねっとの外部連携ポートを指定", type=int)
@click.option("--out_yukacone",default=5000, help="ゆかコネNEOの外部連携ポートを指定", type=int)
#@click.option("--out_illuminate",default=495134, help="未実装",type=int)
@click.option("--filter_lpf_cutoff", default=200, help="ローパスフィルタのカットオフ周波数を設定", type=int)
@click.option("--filter_lpf_cutoff_upper", default=200, help="ローパスフィルタのカットオフ周波数(アッパー)を設定", type=int)
@click.option("--filter_hpf_cutoff", default=200, help="ハイパスフィルタのカットオフ周波数を設定します", type=int)
@click.option("--filter_hpf_cutoff_upper", default=200, help="ハイパスフィルタのカットオフ周波数(アッパー)を設定", type=int)
@click.option("--disable_lpf", default=False, help="ローパスフィルタを使用しません", is_flag=True, type=bool)
@click.option("--disable_hpf", default=False, help="ハイパスフィルタを使用しません", is_flag=True, type=bool)
@click.option("-v", "--verbose", default="0", help="出力ログレベルを指定", type=click.Choice(["0", "1", "2"]))
def main(
    test:bool,
    method:str,
    whisper_model:str,
    whisper_device:str,
    whisper_language:str,
    google_language:str,
    google_timeout:float,
    mic:Optional[int],
    mic_energy:float,
    mic_dynamic_energy:bool,
    mic_pause:float,
    list_devices:bool,
    out:str,
    out_yukarinette:int,
    out_yukacone:int,
#    out_illuminate:int,
    filter_lpf_cutoff:int,
    filter_lpf_cutoff_upper:int,
    filter_hpf_cutoff:int,
    filter_hpf_cutoff_upper:int,
    disable_lpf:bool,
    disable_hpf:bool,
    verbose:str) -> None:

    env = Env(int(verbose))
    if not env.is_exe:
        os.makedirs(env.root, exist_ok=True)
        os.chdir(env.root)

    if list_devices:
        for index, name in enumerate(sr.Microphone.list_microphone_names()):
            print(f"{index} : {name}")
        return

    cancel = CancellationObject()
    try:
        sampling_rate = 16000

        print("認識モデルの初期化")
        audio_model:transcribe.RecognitionModel = {
            "whisper": lambda: transcribe.RecognitionModelWhisper(
                model=whisper_model,
                language=whisper_language,
                device=whisper_device,
                download_root=f"{env.root}{os.sep}.cache"),
            "faster_whisper": lambda:  transcribe.RecognitionModelWhisperFaster(
                model=whisper_model,
                language=whisper_language,
                device=whisper_device,
                download_root=f"{env.root}{os.sep}.cache"),
            "google": lambda: transcribe.RecognitionModelGoogle(
                sample_rate=sampling_rate,
                sample_width=2,
                language=google_language,
                timeout=google_timeout if 0 < google_timeout else None),
            "google_duplex": lambda: transcribe.RecognitionModelGoogleDuplex(
                sample_rate=sampling_rate,
                sample_width=2,
                language=google_language,
                timeout=google_timeout if 0 < google_timeout else None),
        }[method]()
        env.tarce(lambda: print(f"#認識モデルは{type(audio_model)}を使用"))

        print("マイクの初期化")
        mc = Mic(
            sampling_rate,
            mic_energy,
            mic_pause,
            mic_dynamic_energy,
            mic)
        print(f"マイクは{mc.device_name}を使用します")

        outputer:output.RecognitionOutputer = {
            "print": lambda: output.PrintOutputer(),
            "yukarinette": lambda: output.YukarinetteOutputer(f"ws://localhost:{out_yukarinette}", cancel),
            "yukacone": lambda: output.YukaconeOutputer(f"ws://localhost:{out_yukacone}", cancel),
#            "illuminate": lambda: output.IlluminateSpeechOutputer(f"ws://localhost:{out_illuminate}", cancel),
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

            env.tarce(lambda: print(f"#録音データ取得(time={datetime.datetime.now()}, pcm={(int)(len(data)/2)})"))
            try:
                r = env.performance(lambda: audio_model.transcribe(filter(np.frombuffer(data, np.int16).flatten())))
                if r.result[0] not in ["", " ", "\n", None]:
                    env.debug(lambda: print(f"認識時間[{r.time}ms]", end=": "))
                    outputer.output(r.result[0])
                if not r[1] is None:
                    env.tarce(lambda: print(f"{r.result[1]}"))
            except transcribe.TranscribeException as e:
                if e.inner is None:
                    print(e.message)
                else:
                    if isinstance(e.inner, urlerr.HTTPError) or isinstance(e.inner, urlerr.URLError):
                        env.debug(lambda: print(e.message))
                    if isinstance(e.inner, google.UnknownValueError):
                        er = cast(google.UnknownValueError, e.inner)
                        if er.raw_data is None:
                            env.tarce(lambda: print(f"#{e.message}"))
                        else:
                            env.tarce(lambda: print(f"#{e.message}\r\n{cast(google.UnknownValueError, e.inner).raw_data}"))
            except output.WsOutputException as e:
                print(e.message)
                if not e.inner is None:
                    env.tarce(lambda: print(f"# => {type(e.inner)}{e.inner}"))
            except Exception as e:
                print(f"!!!!意図しない例外({type(e)}:{e})!!!!")
            env.tarce(lambda: print(f"#認識処理終了(time={datetime.datetime.now()})"))

        print("認識中…")
        if test:
            mc.listen(onrecord)
        else:
            mc.listen_loop(onrecord, cancel)
    except KeyboardInterrupt:
        cancel.cancel()
        print("ctrl+c")
    sys.exit()


if __name__ == "__main__":
    main() # type: ignore
