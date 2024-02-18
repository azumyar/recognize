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
from typing import Any, Callable, Iterable, Optional, NamedTuple

from src import Logger, Enviroment, db2rms, rms2db
import src.mic
import src.recognition as recognition
import src.output as output
import src.val as val
import src.google_recognizers as google
import src.exception
from src.cancellation import CancellationObject
from src.filter import *


class Record(NamedTuple):
    """
    録音設定
    """
    is_record:bool
    file:str
    directory:str

@click.command()
@click.option("--test", default="", help="テストを行います",type=click.Choice(val.ARG_CHOICE_TEST))
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

@click.option("--mic_energy", default=None, help="互換性のため残されています", type=float)
@click.option("--mic_ambient_noise_to_energy", default=None, help="互換性のため残されています", is_flag=True, type=bool)
@click.option("--mic_dynamic_energy", default=None, is_flag=True, help="互換性のため残されています", type=bool)
@click.option("--mic_dynamic_energy_ratio", default=None, help="互換性のため残されています", type=float)
@click.option("--mic_dynamic_energy_adjustment_damping", default=None, help="-", type=float)
@click.option("--mic_dynamic_energy_min", default=None, help="互換性のため残されています", type=float)

@click.option("--mic_db_threshold", default=rms2db(300), help="設定した値より小さい音を無言として扱う閾値", type=float)
@click.option("--mic_ambient_noise_to_db", default=False, help="起動時の場合環境音から--mic_db_thresholdを変更します", is_flag=True, type=bool)
@click.option("--mic_dynamic_db", default=False, is_flag=True, help="環境音に基づいて無音閾値を動的に変更します", type=bool)
@click.option("--mic_dynamic_db_ratio", default=None, help="-", type=float)
@click.option("--mic_dynamic_db_adjustment_damping", default=None, help="-", type=float)
@click.option("--mic_dynamic_db_min", default=rms2db(100), help="環境音から無音閾値を変更する場合この値より下がることはありません", type=float)

@click.option("--mic_pause", default=0.8, help="無音として認識される秒数を指定します", type=float)
@click.option("--mic_phrase", default=None, help="発話音声として認識される最小秒数", type=float)
@click.option("--mic_non_speaking", default=None, help="-", type=float)
@click.option("--mic_sampling_rate", default=16000, help="-", type=int)

@click.option("--mic_listen_interval", default=0.25, help="マイク監視ループで1回あたりのマイクデバイス監視間隔(秒)", type=float)
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
@click.option(val.ARG_NAME_VERBOSE, default=val.ARG_DEFAULT_VERBOSE, help="出力ログレベルを指定", type=click.Choice(val.ARG_CHOICE_VERBOSE))
@click.option(val.ARG_NAME_LOG_FILE, default=val.ARG_DEFAULT_LOG_FILE, help="ログファイルの出力ファイル名を指定します", type=str)
@click.option(val.ARG_NAME_LOG_DIRECTORY, default=val.ARG_DEFAULT_LOG_DIRECTORY, help="ログ格納先のディレクトリを指定します", type=str)
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

    mic_energy:Optional[float],
    mic_ambient_noise_to_energy:Optional[bool],
    mic_dynamic_energy:Optional[bool],
    mic_dynamic_energy_ratio:Optional[float],
    mic_dynamic_energy_adjustment_damping:Optional[float],
    mic_dynamic_energy_min:Optional[float],
    
    mic_db_threshold:float,
    mic_ambient_noise_to_db:bool,
    mic_dynamic_db:bool,
    mic_dynamic_db_ratio:Optional[float],
    mic_dynamic_db_adjustment_damping:Optional[float],
    mic_dynamic_db_min:float,

    mic_pause:float,
    mic_phrase:Optional[float],
    mic_non_speaking:Optional[float],
    mic_sampling_rate:int,
    mic_listen_interval:float,
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
    log_file:str,
    log_directory:Optional[str],
    record:bool,
    record_file:str,
    record_directory:Optional[str],
    feature:str
    ) -> None:
    from src import ilm_logger, ilm_enviroment

    if not ilm_enviroment.is_exe:
        os.makedirs(ilm_enviroment.root, exist_ok=True)
        os.chdir(ilm_enviroment.root)

    ilm_logger.log([
        "起動",
        f"platform = {platform.platform()}",
        f"python = {sys.version}",
        f"arg = {sys.argv}",
    ])

    if print_mics or list_devices:
        __main_print_mics(ilm_logger, feature)
        return

    cancel = CancellationObject()
    cancel_mp = multiprocessing.Value("i", 1)
    try:
        if record_directory is None:
            record_directory = ilm_enviroment.root
        else:
            os.makedirs(record_directory, exist_ok=True)
        sampling_rate = src.mic.Mic.update_sample_rate(mic, mic_sampling_rate) #16000
        rec = Record(record, record_file, record_directory)

        ilm_logger.print("マイクの初期化")
        def mp_value(db, en): return db if en is None else en
        mp_energy = mp_value(db2rms(mic_db_threshold), mic_energy)
        mp_ambient_noise_to_energy = mp_value(mic_ambient_noise_to_db, mic_ambient_noise_to_energy)
        mp_dynamic_energy = mp_value(mic_dynamic_db, mic_dynamic_energy)
        mp_dynamic_energy_ratio = mp_value(mic_dynamic_db_ratio, mic_dynamic_energy_ratio)
        mp_dynamic_energy_adjustment = mp_value(mic_dynamic_db_adjustment_damping, mic_dynamic_energy_adjustment_damping)
        mp_dynamic_energy_min = mp_value(db2rms(mic_dynamic_db_min), mic_dynamic_energy_min)
        mc = src.mic.Mic(
            sampling_rate,
            mp_ambient_noise_to_energy,
            mp_energy,
            mic_pause,
            mp_dynamic_energy,
            mp_dynamic_energy_ratio,
            mp_dynamic_energy_adjustment,
            mp_dynamic_energy_min,
            mic_phrase,
            mic_non_speaking,
            mic_listen_interval,
            mic)
        ilm_logger.print(f"マイクは{mc.device_name}を使用します")
        ilm_logger.debug(f"指定音圧閾値　 : {rms2db(mp_energy):.2f}")
        ilm_logger.debug(f"現在の音圧閾値 : {rms2db(mc.current_param.energy_threshold):.2f}")

        if test == val.TEST_VALUE_MIC:
            __main_test_mic(
                mc,
                rec,
                ilm_logger,
                cancel,
                feature)
        elif test == val.TEST_VALUE_AMBIENT:
            __main_test_ambient(
                mc,
                3.0,
                ilm_logger,
                feature)
        else:
            ilm_logger.print("認識モデルの初期化")
            recognition_model:recognition.RecognitionModel = {
                val.METHOD_VALUE_WHISPER: lambda: recognition.RecognitionModelWhisper(
                    model=whisper_model,
                    language=whisper_language,
                    device=whisper_device,
                    download_root=f"{ilm_enviroment.root}{os.sep}.cache"),
                val.METHOD_VALUE_WHISPER_FASTER: lambda:  recognition.RecognitionModelWhisperFaster(
                    model=whisper_model,
                    language=whisper_language,
                    device=whisper_device,
                    download_root=f"{ilm_enviroment.root}{os.sep}.cache"),
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
            ilm_logger.debug(f"#認識モデルは{type(recognition_model)}を使用")

            outputer:output.RecognitionOutputer = {
                val.OUT_VALUE_PRINT: lambda: output.PrintOutputer(),
                val.OUT_VALUE_YUKARINETTE: lambda: output.YukarinetteOutputer(f"ws://localhost:{out_yukarinette}", lambda x: ilm_logger.info(x)),
                val.OUT_VALUE_YUKACONE: lambda: output.YukaconeOutputer(f"ws://localhost:{output.YukaconeOutputer.get_port(out_yukacone)}", lambda x: ilm_logger.info(x)),
                #val.OUT_VALUE_ILLUMINATE: lambda: output.IlluminateSpeechOutputer(f"ws://localhost:{out_illuminate}"),
            }[out]()
            ilm_logger.debug(f"#出力は{type(outputer)}を使用")

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
            ilm_logger.debug(f"#使用音声フィルタ({len(filters)}):")
            for f in filters:
               ilm_logger.debug(f"#{type(f)}")


            ilm_logger.log([
                f"マイク: {mc.device_name}",
                f"{mc.get_mic_info()}",
                f"認識モデル: {type(recognition_model)}",
                f"出力 = {type(outputer)}",
                f"フィルタ = {','.join(list(map(lambda x: f'{type(x)}', filters)))}"
            ])

            ilm_logger.print("認識中…")
            __main_run(
                mc,
                recognition_model,
                outputer,
                filters,
                rec,
                ilm_enviroment,
                cancel,
                test == val.TEST_VALUE_RECOGNITION,
                ilm_logger,
                feature)
    except src.mic.MicInitializeExeception as e:
        ilm_logger.print(e.message)
        ilm_logger.print(f"{type(e.inner)}{e.inner}")
    except KeyboardInterrupt:
        cancel.cancel()
        cancel_mp.value = 0 # type: ignore
        ilm_logger.print("ctrl+c")
    finally:
        pass
    sys.exit()


def __main_print_mics(logger:Logger, _:str) -> None:
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
                logger.print(f"{index} : [{host_api_name}]{name} sample_rate={rate}")            
    finally:
        audio.terminate()

def __main_test_mic(mic:src.mic.Mic, rec:Record, logger:Logger, cancel:CancellationObject, _:str) -> None:
    """
    マイクテスト
    """
    def ontart(index:int) -> None:
        """
        マイク認識開始コールバック関数
        """
        logger.print(f"計測開始 #{str(index).zfill(2)}")

    def onend(index:int, param:src.mic.ListenResultParam|None) -> None:
        """
        マイク認識結果が返るコールバック関数
        """
        def energy(v:float|None) -> float: return v if not v is None else -1.
        if param is None:
            # 認識しなかった場合今のところ何もしない
            return

        pp = mic.current_param
        sec = len(param.pcm) / 2 / pp.sample_rate

        logger.print("認識終了")
        logger.print(f"{round(sec - mic.end_insert_sec, 2)}[秒]発声を認識しました")
        if not param.energy is None:
            logger.print(f"入力　　　 : {rms2db(param.energy):.2f}dB")
            logger.print(f"現在の閾値 : {rms2db(pp.energy_threshold):.2f}dB")
        logger.print(" ")

        logger.log([
            f"認識 #{str(index).zfill(2)}",
            f"PCM    = {round(sec - mic.end_insert_sec, 2)}sec",
            f"dB     = {(rms2db(param.energy) if not param.energy is None else 0):.2f}, threshold={rms2db(pp.energy_threshold):.2f}",
            f"energy = {(param.energy if not param.energy is None else 0):.2f}, threshold={pp.energy_threshold:.2f}"
        ])
        save_wav(rec, index, param.pcm, mic.sample_rate, 2, logger)

    timemax = 5
    prm = mic.current_param
    assert not prm.phrase_threshold is None
    assert not prm.non_speaking_duration is None

    logger.log([
        "マイクテスト起動"
        f"マイク: {mic.device_name}",
        f"{mic.get_log_info()}",
    ])

    logger.print("マイクテストを行います")
    logger.print(f"{int(timemax)}秒間マイクを監視し音を拾った場合その旨を表示します")
    logger.print(f"使用マイク　　　　　　 : {prm.device_name}")
    logger.print(f"音圧閾値　　　　　　　 : {rms2db(prm.energy_threshold):.2f}")
    logger.print(f"最低発声時間　　　　　 : {prm.phrase_threshold:.2f}")
    logger.print(f"発生後無音時間　　　　 : {prm.pause_threshold:.2f}")
    logger.print(f"環境音で音圧閾値の変更 : {prm.dynamic_energy}")
    logger.print(f"音圧閾値の最小保証値　 : {rms2db(prm.dynamic_energy_min):.2f}")
    logger.print("終了する場合はctr+cを押してください")
    logger.print("")
    mic.test_mic(timemax, ontart, onend, cancel)
    return


def __main_test_ambient(
    mic:src.mic.Mic,
    timeout:float,
    logger:Logger,
    _:str) -> None:

    import speech_recognition as sr
    import math
    import re
    repatter = re.compile("^.+\\.0$")
    def fmt(txt:str, padding:int) -> str:
        text_len = len(txt)
        pad = padding - text_len #+ ((text_len % 2 - 1) * -1)
        return txt + "".ljust(max(0, pad), "　")
    def value(v:float|None, default:float) -> float: return v if not v is None else default
    def calc_threshold(energy:float|Iterable[float], threshold:float) -> float:
        if isinstance(energy, Iterable):
            th = threshold
            for en in energy:
                th = calc_threshold(en ,th)
            return th
        else:
            damping = dynamic_energy_adjustment_damping_ ** seconds_per_buffer
            target_energy = energy * dynamic_energy_ratio_
            return threshold * damping + target_energy * (1 - damping)

    def add(lst:list[float], v:tuple[float, list[float]], max:int) -> None:
        lst.append(v[0])
        if max <= len(lst):
            lst.pop(1)

    def avg(lst:list[float]) -> float:
        return sum(lst) / len(lst)

    def round_s(f:float) -> str:
        '''
        round(f, 2) の結果がN.0の場合 str(int(f)) とし末尾小数点を付与しない形に成形する
        '''
        def r():
            s = str(round(f, 2))
            if repatter.match(s) is None:
                return s
            else:
                return str(int(f))
        t = r()
        return r().rjust(len(t) % 2)

    init_mic_param = mic.initilaze_param
    max_list:int = max(math.ceil(60 / timeout), 10) # 1分サンプル/最低10レコード保障 

    logger.log("環境音測定機能を起動")
    logger.print("環境音の測定を行います")
    logger.print("発言はせず、実際に利用する際にとる行動をおこなってください。")
    logger.print(f"この機能は次の3種の音圧を取り扱います。")
    logger.print(f"A. {round_s(timeout)}[秒]おきに表示する直近の音圧")
    logger.print(f"B. 中期間M[分]保存した音圧")
    logger.print(f"C. (A.)で計測した音圧の最大値と最小値")
    logger.print(f" ")
    logger.print(f"そして音圧閾値を提案します。")
    logger.print(f"A. 計測期間中更新し続ける今使うのに適している閾値")
    logger.print(f"B. 初期閾値から{round_s(timeout * max_list / 60)}[分]の音圧平均で求められる閾値")
    logger.print(f"C. 初期閾値と計測中最大/最小を記録した音圧で求められる閾値")
    logger.print(" ")
    logger.print("終了は ctrl+c を押してください。")
    try:
        list_energys:list[float] = []
        energy_max:tuple[float, list[float]] = (-1, [])
        energy_min:tuple[float, list[float]] = (-1, [])
        energy_threshold = init_mic_param.energy_threshold
        while True:
            elapsed_time = 0
            energy_history:list[float] = []
            dynamic_energy_ratio_ = value(init_mic_param.dynamic_energy_ratio, 1.5)
            dynamic_energy_adjustment_damping_ = value(init_mic_param.dynamic_energy_adjustment_damping, 0.15)

            with mic as source:
                logger.print(f"{round_s(timeout)}[秒]マイクから環境音を採取します")
                logger.print(f"マイク情報　　 : {init_mic_param.device_name}, {source.SAMPLE_RATE}[Hz]/{source.SAMPLE_WIDTH * 8}[bit]")
                logger.print(f"サイクル標本量 : {source.CHUNK}[sample]")
                seconds_per_buffer = float(source.CHUNK) / source.SAMPLE_RATE
                while elapsed_time <= timeout:
                    elapsed_time += seconds_per_buffer

                    buffer = source.stream.read(source.CHUNK) # type: ignore
                    if len(buffer) == 0:
                        break 
                    energy = audioop.rms(buffer, source.SAMPLE_WIDTH)
                    energy_history.append(energy)
                    energy_threshold = calc_threshold(energy, energy_threshold)
                
                energy_avg = (sum(energy_history)/len(energy_history), energy_history)
                add(list_energys, energy_avg, max_list)
                energy_max = max(energy_max, energy_avg, key=lambda x: x[0])
                energy_min = min(energy_min, energy_avg, key=lambda x: x[0]) if 0 <= energy_min[0] else energy_avg
                recernt_energy_avg = avg(list_energys)
                logger.print("完了")

                # ここから結果表示用の計算と主にフォーマット処理
                format_length = 10
                sep_length = 80
                s_energy_avg = f"{rms2db(energy_avg[0]):.2f}"
                s_energy_threshold = f"{rms2db(energy_threshold):.2f}"
                s_energy_max = f"{rms2db(energy_max[0]):.2f}"
                s_energy_min = f"{rms2db(energy_min[0]):.2f}"
                s_recernt_energy_avg = f"{rms2db(recernt_energy_avg):.2f}"
                en_length = max(
                    len(s_energy_avg),
                    len(s_energy_threshold),
                    len(s_energy_max),
                    len(s_energy_min),
                    len(s_recernt_energy_avg)
                )
                s_th_max = f"{rms2db(calc_threshold(energy_max[1], init_mic_param.energy_threshold)):.2f}"
                s_th_min = f"{rms2db(calc_threshold(energy_min[1], init_mic_param.energy_threshold)):.2f}"
                s_recernt_th = f"{rms2db(calc_threshold(list_energys, init_mic_param.energy_threshold)):.2f}"
                th_length = max(
                    len(s_th_max),
                    len(s_th_min),
                    len(s_recernt_th)
                )
                out = [
                    "".ljust(sep_length, "-"),
                    fmt(f"計測音圧", format_length) + f": {s_energy_avg.rjust(en_length)}",
                    fmt(f"計算閾値", format_length) + f": {s_energy_threshold.rjust(en_length)}",
                    "".ljust(sep_length, "-"),
                    fmt(f"計測中最大の音圧", format_length) +  f": {s_energy_max.rjust(en_length)}, {s_th_max.rjust(th_length)}",
                    fmt(f"計測中最小の音圧", format_length) +  f": {s_energy_min.rjust(en_length)}, {s_th_min.rjust(th_length)}",
                    fmt(f"直近を平均した音圧", format_length) + f": {s_recernt_energy_avg.rjust(en_length)}, {s_recernt_th.rjust(th_length)}",
                    "".ljust(sep_length, "-"),
                ]
                logger.print(os.linesep.join(out))
                out.append(os.linesep.join([
                    "RAW DATA",
                    "計測" + f"{energy_avg[0]:.2f}, {energy_threshold:.2f}",
                    "最大" + f"{energy_max[0]:.2f}, {calc_threshold(energy_max[1], init_mic_param.energy_threshold):.2f}",
                    "最低" + f"{energy_min[0]:.2f}, {calc_threshold(energy_min[1], init_mic_param.energy_threshold):.2f}",
                    "平均" + f"{recernt_energy_avg:.2f}, {calc_threshold(list_energys, init_mic_param.energy_threshold):.2f}",
                ]))
                logger.log(out)
                logger.print(" ")
    except Exception as e:
        logger.error([
            f"!!!!意図しない例外({type(e)}:{e})!!!!",
            f"{type(e)}:{e}",
            traceback.format_exc()
        ])


def __main_run(
    mic:src.mic.Mic,
    recognition_model:recognition.RecognitionModel,
    outputer:output.RecognitionOutputer,
    filters:list[NoiseFilter],
    record:Record,
    env:Enviroment,
    cancel:CancellationObject,
    is_test:bool,
    logger:Logger,
    _:str) -> None:
    """
    メイン実行
    """

    thread_pool = ThreadPoolExecutor(max_workers=1)
    def onrecord(index:int, param:src.mic.ListenResultParam) -> None:
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
        class PerformanceResult(NamedTuple):
            result:Any
            time:float
        def performance(func:Callable[[], Any]) ->  PerformanceResult:
            """
            funcを実行した時間を計測
            """
            import time
            start = time.perf_counter() 
            r = func()
            return PerformanceResult(r, time.perf_counter()-start)
        log_info_mic = mic.get_log_info()
        log_info_recognition = recognition_model.get_log_info()

        insert:str
        if 0 < mic.end_insert_sec:
            insert = f", {round(mic.end_insert_sec, 2)}s挿入"
        else:
            insert = ""
        if not param.energy is None:
            insert = f"{insert}, dB={rms2db(param.energy):.2f}"
        data = param.pcm
        pcm_sec = len(data) / 2 / mic.sample_rate
        logger.debug(f"#録音データ取得(#{index}, time={dt.datetime.now()}, pcm={(int)(len(data)/2)}, {round(pcm_sec, 2)}s{insert})")
        r = PerformanceResult(None, -1)
        log_exception:Exception | None = None
        try:
            save_wav(record, index, data, mic.sample_rate, 2, logger)
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

            r = performance(lambda: recognition_model.transcribe(filter(np.frombuffer(d, np.int16).flatten())))
            assert(isinstance(r.result, recognition.TranscribeResult)) # ジェネリクス使った型定義の方法がわかってないのでassert置いて型を確定させる
            if r.result.transcribe not in ["", " ", "\n", None]:
                logger.notice(f"認識時間[{round(r.time, 2)}s],PCM[{round(pcm_sec, 2)}s],{round(r.time/pcm_sec, 2)}tps", end=": ")
                outputer.output(r.result.transcribe)
            if not r.result.extend_data is None:
                logger.debug(f"{r.result.extend_data}")
        except recognition.TranscribeException as e:
            log_exception = e
            if e.inner is None:
                logger.info(e.message)
            else:
                if isinstance(e.inner, urlerr.HTTPError) or isinstance(e.inner, urlerr.URLError):
                    logger.notice(e.message)
                elif isinstance(e.inner, google.UnknownValueError):
                    raw = e.inner.raw_data
                    if raw is None:
                        logger.debug(f"#{e.message}")
                    else:
                        logger.debug(f"#{e.message}{os.linesep}{raw}")
                else:
                    logger.debug(f"#{e.message}")
                    logger.debug(f"#{type(e.inner)}:{e.inner}")
        except output.WsOutputException as e:
            log_exception = e
            logger.info(e.message)
            if not e.inner is None:
                logger.debug(f"# => {type(e.inner)}:{e.inner}")
        except Exception as e:
            log_exception = e
            logger.error([
                f"!!!!意図しない例外!!!!",
                f"{type(e)}:{e}",
                traceback.format_exc()
            ])
        for it in [("", mic.get_verbose(env.verbose)), ("", recognition_model.get_verbose(env.verbose))]:
            pass
        logger.debug(f"#認識処理終了(#{index}, time={dt.datetime.now()})")

        # ログ出力
        try:
            log_transcribe = " - "
            log_time = " - "
            log_exception_s = " - "
            log_insert:str
            if not r.result is None:
                if r.result[0] not in ["", " ", "\n", None]:
                    log_transcribe = r.result[0]
                if not r.result[1] is None:
                    log_transcribe = f"{log_transcribe}\n{r.result[1]}"
                log_time = f"{round(r.time, 2)}s {round(r.time/1000.0/pcm_sec, 2)}tps"
            if not log_exception is None:
                log_exception_s = f"{type(log_exception)}"
                if isinstance(log_exception, src.exception.IlluminateException):
                    log_exception_s = f"{log_exception}:{log_exception.message}\ninner = {type(log_exception.inner)}:{log_exception.inner}"
                else:
                    log_exception_s = f"{log_exception}:{log_exception}"
                if isinstance(log_exception, recognition.TranscribeException):
                    log_transcribe = " -失敗- "
            if 0 < mic.end_insert_sec:
                log_insert = f"({round(mic.end_insert_sec, 2)}s挿入)"
            else:
                log_insert = ""
            if not param.energy is None:
                log_insert = f"{log_insert}, energy={param.energy:.2f};{rms2db(param.energy):.2f}dB"

            logger.log([
                f"認識処理　　　: #{index}",
                f"録音情報　　　: {round(pcm_sec, 2)}s{log_insert}, {(int)(len(data)/2)}sample / {mic.sample_rate}Hz",
                f"認識結果　　　: {log_transcribe}",
                f"認識時間　　　: {log_time}",
                f"例外情報　　　: {log_exception_s}",
                f"マイク情報　　: {log_info_mic}",
                f"認識モデル情報: {log_info_recognition}",
            ])
        except Exception as e_: # eにするとPylanceの動きがおかしくなるので名前かえとく
            logger.error([
                f"!!!!ログ出力例外!!!!",
                f"({type(e_)}:{e_})",
                traceback.format_exc()
             ])

    def onrecord_async(index:int, data:src.mic.ListenResultParam) -> None:
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


def save_wav(record:Record, index:int, data:bytes, sampling_rate:int, sample_width:int, logger:Logger) -> None:
    """
    音声データをwavに保存
    """
    if record.is_record:
        try:
            with open(f"{record.directory}{os.sep}{record.file}-{str(index).zfill(4)}.wav", "wb") as fout:      
                fout.write(speech_recognition.AudioData(data, sampling_rate, 2).get_wav_data())
        except OSError as e:
            logger.error([
                "##########################",
                "wavファイルの保存に失敗しました",
                str(e),
                traceback.format_exc(),
                "##########################"
            ])

if __name__ == "__main__":
    main() # type: ignore
