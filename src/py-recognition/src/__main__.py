#!/usr/bin/env python3

import os
import sys
import platform
import traceback
import click
import torch
import speech_recognition
import urllib.error as urlerr
import multiprocessing
import audioop
import numpy as np
import datetime as dt
from concurrent.futures import ThreadPoolExecutor
from typing import cast, Optional, NamedTuple

import src.mic as mic_
import src.recognition as recognition
import src.output as output
import src.val as val
import src.google_recognizers as google
import src.env as env_
import src.mp
import src.exception
from src.cancellation import CancellationObject
from src.filter import *
from src.interop import print

class Record(NamedTuple):
    """
    録音設定
    """
    is_record:bool
    file:str
    directory:str

class Logger:
    def __init__(self, dir:str) -> None:
        self.__dir = dir
        self.__file_io = self.__file(dir)

    def __file(self, dir:str):
        return open(f"{dir}{os.sep}log.txt", "w", encoding="UTF-8")

    def log(self, arg:object) -> None:
        time = dt.datetime.now()
        s:str
        if hasattr(arg, "__iter__"):
            s = "\n".join(list(map(lambda x: f"{x}", arg))) # type: ignore
        else:
            s = f"{arg}"
        self.__file_io.write(f"{time}\n{s}\n\n")
        self.__file_io.flush()



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
@click.option("--google_duplex_parallel", default=False, help="(google_duplexのみ)複数並列リクエストを投げエラーの抑制を図ります", is_flag=True, type=bool)
@click.option("--google_duplex_parallel_max", default=None, help="(google_duplexのみ)複数並列リクエスト数増減時の最大並列数", type=int)
@click.option("--google_duplex_parallel_reduce_count", default=None, help="(google_duplexのみ)増加した並列数を減少するために必要な成功数", type=int)
@click.option("--mic", default=None, help="使用するマイクのindex", type=int)
@click.option("--mic_energy", default=300, help="設定した値より小さいマイク音量を無音として扱います", type=float)
@click.option("--mic_dynamic_energy", default=False, is_flag=True, help="Trueの場合周りの騒音に基づいてマイクのエネルギーレベルを動的に変更します", type=bool)
@click.option("--mic_dynamic_energy_ratio", default=1.5, help="--mic_dynamic_energyで--mic_energyを変更する場合の最小係数", type=float)
@click.option("--mic_dynamic_energy_min", default=100, help="--mic_dynamic_energyを指定した場合動的設定される--mic_energy最低値", type=float)
@click.option("--mic_pause", default=0.8, help="無音として認識される秒数を指定します", type=float)
@click.option("--mic_phrase", default=None, help="発話音声として認識される最小秒数", type=float)
@click.option("--mic_non_speaking", default=None, help="-", type=float)
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
@click.option("--log", default=False, help="-", is_flag=True, type=bool)
@click.option("--record",default=False, help="録音した音声をファイルとして出力します", is_flag=True, type=bool)
@click.option("--record_file", default="record", help="録音データの出力ファイル名を指定します", type=str)
@click.option("--record_directory", default=None, help="録音データの出力先ディレクトリを指定します", type=str)
@click.option("--feature", default="", help="-", type=str) 
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
    google_duplex_parallel:bool,
    google_duplex_parallel_max:Optional[int],
    google_duplex_parallel_reduce_count:Optional[int],
    mic:Optional[int],
    mic_energy:float,
    mic_dynamic_energy:bool,
    mic_dynamic_energy_ratio:float,
    mic_dynamic_energy_min:float,
    mic_pause:float,
    mic_phrase:Optional[float],
    mic_non_speaking:Optional[float],
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
    verbose:str,
    log:bool,
    record:bool,
    record_file:str,
    record_directory:Optional[str],
    feature:str
    ) -> None:


    env = env_.Env(int(verbose))
    if not env.is_exe:
        os.makedirs(env.root, exist_ok=True)
        os.chdir(env.root)

    logger = Logger(env.root)
    logger.log([
        "起動",
        f"platform = {platform.platform()}",
        f"python = {sys.version}",
        f"arg = {sys.argv}",
    ])

    if print_mics or list_devices:
        __main_print_mics(feature)
        return
    
    if is_feature(feature, "energy"):
        __main_print_energy(
            mic,
            mic_sampling_rate,
            mic_dynamic_energy_ratio,
            mic_dynamic_energy_adjustment_damping,
            3.0,
            feature)
        pass

    cancel = CancellationObject()
    cancel_mp = multiprocessing.Value("i", 1)
    try:
        if record_directory is None:
            record_directory = env.root
        else:
            os.makedirs(record_directory, exist_ok=True)
        sampling_rate = mic_.Mic.update_sample_rate(mic, mic_sampling_rate) #16000
        rec = Record(record, record_file, record_directory)

        print("マイクの初期化")
        mc = mic_.Mic(
            sampling_rate,
            mic_energy,
            mic_pause,
            mic_dynamic_energy,
            mic_dynamic_energy_ratio,
            mic_dynamic_energy_min,
            mic_phrase,
            mic_non_speaking,
            mic)
        print(f"マイクは{mc.device_name}を使用します")
        env.tarce(lambda: print(f"input energy={mic_energy}"))
        env.tarce(lambda: print(f"current energy=-"))

        if test == val.TEST_VALUE_MIC:
            __main_test_mic(mc, rec, cancel, feature)
        elif is_feature(feature, "mp"):
            print("実験的機能：マルチプロセスでマイクの監視を行います")
            print("--recordは実装されていません")

            q = multiprocessing.Queue()
            p = multiprocessing.Process(target=src.mp.main_feature_mp, args=(
                q,
                cancel_mp,
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
                0,
                verbose,
                feature))
            p.daemon = True
            p.start()
            mc.listen_loop_mp(q, cancel_mp)
        else:
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
                    is_parallel_run=google_duplex_parallel,
                    parallel_max=google_duplex_parallel_max,
                    parallel_reduce_count=google_duplex_parallel_reduce_count),
            }[method]()
            env.tarce(lambda: print(f"#認識モデルは{type(recognition_model)}を使用"))

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


            logger.log([
                f"マイク: {mc.device_name}",
                f"{mc.get_mic_info()}",
                f"認識モデル: {type(recognition_model)}",
                f"出力 = {type(outputer)}",
                f"フィルタ = {','.join(list(map(lambda x: f'{type(x)}', filters)))}"
            ])

            print("認識中…")
            __main_run(
                mc,
                recognition_model,
                outputer,
                filters,
                rec,
                env,
                cancel,
                test == val.TEST_VALUE_RECOGNITION,
                logger,
                feature)
    except mic_.MicInitializeExeception as e:
        print(e.message)
        print(f"{type(e.inner)}{e.inner}")
    except KeyboardInterrupt:
        cancel.cancel()
        cancel_mp.value = 0 # type: ignore
        print("ctrl+c")
    finally:
        pass
    sys.exit()


def __main_print_mics(_:str) -> None:
    """
    マイク情報出力
    """
    audio = speech_recognition.Microphone.get_pyaudio().PyAudio()
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

def __main_print_energy(
        device:int|None,
        sample_rate:int,
        dynamic_energy_ratio:float|None,
        dynamic_energy_adjustment_damping:float|None,
        timeout:float,
        _:str) -> None:
    import speech_recognition as sr
    def value(v:float|None, default:float) -> float: return v if not v is None else default

    rate = mic_.Mic.update_sample_rate(device, sample_rate)
    mic = sr.Microphone(sample_rate = rate, device_index = device)

    print("feature function:energy")
    print("exit ctrl+c")
    try:
        while True:
            elapsed_time = 0
            energy_threshold = 0.0
            energy_total = 0.0
            dynamic_energy_ratio_ = value(dynamic_energy_ratio, 1.5)
            dynamic_energy_adjustment_damping_ = value(dynamic_energy_adjustment_damping, 0.15)

            print(f"start record {round(timeout, 2)} sec")
            with mic as source:
                count = 0
                seconds_per_buffer = float(source.CHUNK) / source.SAMPLE_RATE
                while elapsed_time <= timeout:
                    count += 1
                    elapsed_time += seconds_per_buffer

                    buffer = source.stream.read(source.CHUNK) # type: ignore
                    if len(buffer) == 0:
                        break 
                    energy = audioop.rms(buffer, source.SAMPLE_WIDTH)
                    energy_total += energy
                    damping = dynamic_energy_adjustment_damping_ ** seconds_per_buffer 
                    target_energy = energy * dynamic_energy_ratio_
                    energy_threshold = energy_threshold * damping + target_energy * (1 - damping)
                
                print("done.")
                print("--------------------------------------")
                print(f"input energy average       : {round(energy_total / count, 2)}")
                print(f"calcurate energy threshold : {round(energy_threshold, 2)}")
                print("--------------------------------------")
                print("")
    except Exception as e:
        print(f"except Exception as {type(e)}")
        print(e)
        print(traceback.format_exc())
    finally:
        sys.exit()



def __main_test_mic(mic:mic_.Mic, rec:Record, cancel:CancellationObject, _:str) -> None:
    """
    マイクテスト
    """
    def onrecord(index:int, data:bytes) -> None:
        """
        マイク認識データが返るコールバック関数
        """
        save_wav(rec, index, data, mic.sample_rate)

    mic.test_mic(cancel, onrecord)
    return

def __main_run(
    mic:mic_.Mic,
    recognition_model:recognition.RecognitionModel,
    outputer:output.RecognitionOutputer,
    filters:list[NoiseFilter],
    record:Record,
    env:env_.Env,
    cancel:CancellationObject,
    is_test:bool,
    logger:Logger,
    _:str) -> None:
    """
    メイン実行
    """

    thread_pool = ThreadPoolExecutor(max_workers=1)
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

        insert:str
        if 0 < mic.end_insert_sec:
            insert = f", {round(mic.end_insert_sec, 2)}s挿入"
        else:
            insert = ""
        pcm_sec = len(data) / 2 / mic.sample_rate
        env.tarce(lambda: print(f"#録音データ取得(#{index}, time={dt.datetime.now()}, pcm={(int)(len(data)/2)}, {round(pcm_sec, 2)}s{insert})"))
        r = env_.PerformanceResult(None, -1)
        ex:Exception | None = None
        try:
            save_wav(record, index, data, mic.sample_rate)
            if recognition_model.required_sample_rate is None or mic.sample_rate == recognition_model.required_sample_rate:
                d = data
            else:
                d, _ = audioop.ratecv(
                    data,
                    2, # sample_width
                    1,
                    mic.sample_rate,
                    recognition_model.required_sample_rate,
                    None)

            r = env.performance(lambda: recognition_model.transcribe(filter(np.frombuffer(d, np.int16).flatten())))
            if r.result[0] not in ["", " ", "\n", None]:
                if env.is_trace:
                    env.debug(lambda: print(f"認識時間[{r.time}ms],PCM[{round(pcm_sec, 2)}s],{round(r.time/1000.0/pcm_sec, 2)}tps", end=": "))
                else:
                    env.debug(lambda: print(f"認識時間[{r.time}ms]", end=": "))
                outputer.output(r.result[0])
            if not r.result[1] is None:
                env.tarce(lambda: print(f"{r.result[1]}"))
        except recognition.TranscribeException as e:
            ex = e
            if e.inner is None:
                print(e.message)
            else:
                if isinstance(e.inner, urlerr.HTTPError) or isinstance(e.inner, urlerr.URLError):
                    env.debug(lambda: print(e.message))
                elif isinstance(e.inner, google.UnknownValueError):
                    raw = e.inner.raw_data
                    if raw is None:
                        env.tarce(lambda: print(f"#{e.message}"))
                    else:
                        env.tarce(lambda: print(f"#{e.message}\r\n{raw}"))
                else:
                    env.tarce(lambda: print(f"#{e.message}"))
                    env.tarce(lambda: print(f"#{type(e.inner)}:{e.inner}"))
        except output.WsOutputException as e:
            ex = e
            print(e.message)
            if not e.inner is None:
                env.tarce(lambda: print(f"# => {type(e.inner)}:{e.inner}"))
        except Exception as e:
            ex = e
            print(f"!!!!意図しない例外({type(e)}:{e})!!!!")
            print(traceback.format_exc())
        for it in [("", mic.get_verbose(env.verbose)), ("", recognition_model.get_verbose(env.verbose))]:
            pass
        env.tarce(lambda: print(f"#認識処理終了(#{index}, time={dt.datetime.now()})"))

        # ログ出力
        try:
            log_transcribe:str
            log_time = " - "
            log_exception = " - "
            if ex is None:
                log_transcribe = " - "
                if r.result[0] not in ["", " ", "\n", None]:
                    log_transcribe = r.result[0]
                    if env.is_trace:
                        env.debug(lambda: print(f"認識時間[{r.time}ms],PCM[{round(pcm_sec, 2)}s],{round(r.time/1000.0/pcm_sec, 2)}tps", end=": "))
                    outputer.output(r.result[0])
                if not r.result[1] is None:
                    log_transcribe = f"{log_transcribe}\n{r.result[1]}"
                log_time = f"{r.time}s {round(r.time/1000.0/pcm_sec, 2)}tps"
            else:
                log_transcribe = " -失敗- "
                log_exception = f"{type(ex)}"
                if isinstance(ex, src.exception.IlluminateException):
                    log_exception = f"{log_exception}:{ex.message}\ninner = {type(ex.inner)}:{ex.inner}"
                else:
                    log_exception = f"{log_exception}:{ex}"
                if isinstance(ex, recognition.TranscribeException):
                    pass
            logger.log([
                f"認識処理:#{index}",
                f"録音情報:{round(pcm_sec, 2)}s, {(int)(len(data)/2)}sample / {mic.sample_rate}Hz",
                f"認識結果:{log_transcribe}",
                f"認識時間:{log_time}",
                f"例外情報:{log_exception}",
                f"マイク情報: {mic.get_log_info()}",
                f"認識モデル情報: {recognition_model.get_log_info()}",
            ])
        except Exception as e_: # eにするとPylanceの動きがおかしくなるので名前かえとく
            print(f"!!!!ログ出力例外({type(e_)}:{e_})!!!!")
            print(traceback.format_exc())

    def onrecord_async(index:int, data:bytes) -> None:
        """
        マイク認識データが返るコールバック関数の非同期版
        """
        thread_pool.submit(onrecord, index, data)

    try:
        if is_test:
            mic.listen(onrecord)
        else:
            mic.listen_loop(onrecord_async, cancel)
    finally:
        thread_pool.shutdown()

def is_feature(feature:str, func:str) -> bool:
    """
    featureにfuncが含まれるか判定
    """
    def strip(s:str) -> str:
        return str.strip(s)
    return func in map(strip, feature.split(","))


def save_wav(record:Record, index:int, data:bytes, sampling_rate) -> None:
    """
    音声データをwavに保存
    """
    if record.is_record:
        with open(f"{record.directory}{os.sep}{record.file}-{str(index).zfill(4)}.wav", "wb") as fout:      
            fout.write(speech_recognition.AudioData(data, sampling_rate, 2).get_wav_data())

if __name__ == "__main__":
    multiprocessing.set_start_method("spawn")
    main() # type: ignore
