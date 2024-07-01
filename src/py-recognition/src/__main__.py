#!/usr/bin/env python3

import os
import sys
import platform
import click
import speech_recognition
from typing import Any, Callable, Iterable, Optional, NamedTuple

from src import Logger, Enviroment, db2rms, rms2db, ilm_logger
import src.main_run as main_run
import src.main_test as main_test
import src.mic
import src.recognition as recognition
import src.output as output
import src.val as val
import src.google_recognizers as google
import src.exception
from src.main_common import Record
from src.cancellation import CancellationObject
from src.filter import *

def select_google_tcp(ctx, param, value):
    import src.google_recognizers as google
    if not value or ctx.resilient_parsing:
        return
    
    if value == "urllib":
        ilm_logger.log(f"google tcpは{value}で接続")
        google.recognize_google = google.recognize_google_urllib
        google.recognize_google_duplex = google.recognize_google_duplex_urllib
    elif value == "requests":
        ilm_logger.log(f"google tcpは{value}で接続")
        google.recognize_google = google.recognize_google_requests
        google.recognize_google_duplex = google.recognize_google_duplex_requests
    else:
        ilm_logger.error(f"unknown option-value: {value}")


def print_mics(ctx, param, value):
    """
    マイク情報出力
    """
    from src import ilm_logger

    if not value or ctx.resilient_parsing:
        return

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
                ilm_logger.print(f"{index} : [{host_api_name}]{name} sample_rate={rate}")            
    finally:
        audio.terminate()
        ctx.exit()

def __available_cuda() -> str:
    try:
        import torch # type: ignore
    except ImportError:
        return "cpu"
    else:
        return "cuda" if torch.cuda.is_available() else "cpu"


def __whiper_help(s:str) -> str:
    if not val.SUPPORT_WHISPER:
        return "サポートしていません"
    return s

@click.command()
@click.option("--test", default="", help="テストを行います",type=click.Choice(val.ARG_CHOICE_TEST))

@click.option("--method", default=val.DEFALUT_METHOD_VALUE, help="使用する認識方法", type=click.Choice(val.ARG_CHOICE_METHOD))
@click.option("--whisper_model", default="medium", help=__whiper_help("(whisper)使用する推論モデル"), type=str) # type=click.Choice(["tiny","base", "small","medium","large","large-v2","large-v3"])
@click.option("--whisper_device", default=__available_cuda(), help=__whiper_help("(whisper)使用する演算装置"), type=click.Choice(["cpu","cuda"]))
@click.option("--whisper_device_index", default=0, help=__whiper_help("(whisper)使用するデバイスindex"), type=int)
@click.option("--whisper_language", default="", help=__whiper_help("(whisper)音声解析対象の言語"), type=click.Choice(val.LANGUAGE_CODES))
@click.option("--google_language", default="ja-JP", help="(google)音声解析対象の言語", type=str)
@click.option("--google_timeout", default=5.0, help="(google)最大認識待ち時間", type=float)
@click.option("--google_convert_sampling_rate", default=False, help="(google)マイク入力を16kに変換します", is_flag=True, type=bool)
@click.option("--google_error_retry", default=1, help="(google)500エラー時にリトライ試行する回数", type=int)
@click.option("--google_duplex_parallel", default=False, help="(google_duplexのみ)複数並列リクエストを投げエラーの抑制を図ります", is_flag=True, type=bool)
@click.option("--google_duplex_parallel_max", default=None, help="(google_duplexのみ)複数並列リクエスト数増減時の最大並列数", type=int)
@click.option("--google_duplex_parallel_reduce_count", default=None, help="(google_duplexのみ)増加した並列数を減少するために必要な成功数", type=int)
@click.option("--google_tcp", default=None, help="-", type=click.Choice(["urllib", "requests"]), callback=select_google_tcp, expose_value=False, is_eager=True)

@click.option("--mic", default=None, help="使用するマイクのindex", type=int)
@click.option("--mic_name", default=None, help="マイクの名前を部分一致で検索します。--micが指定されている場合この指定は無視されます", type=str)
@click.option("--mic_api", default=val.MIC_API_VALUE_MME, help="--mic_nameで検索するマイクのAPIを指定します", type=click.Choice(val.ARG_CHOICE_MIC_API))

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
@click.option("--mic_delay_duration", default=None, help="-", type=float)

@click.option("--out", default=val.OUT_VALUE_PRINT, help="認識結果の出力先", type=click.Choice(val.ARG_CHOICE_OUT))
@click.option("--out_yukarinette",default=49513, help="ゆかりねっとの外部連携ポートを指定", type=int)
@click.option("--out_yukacone",default=None, help="ゆかコネNEOの外部連携ポートを指定", type=int)
@click.option("--out_illuminate",default=495134, help="-",type=int)

@click.option("--filter_lpf_cutoff", default=0, help="ローパスフィルタのカットオフ周波数を設定", type=int)
@click.option("--filter_lpf_cutoff_upper", default=200, help="ローパスフィルタのカットオフ周波数(アッパー)を設定", type=int)
@click.option("--filter_hpf_cutoff", default=0, help="ハイパスフィルタのカットオフ周波数を設定します", type=int)
@click.option("--filter_hpf_cutoff_upper", default=200, help="ハイパスフィルタのカットオフ周波数(アッパー)を設定", type=int)
@click.option("--disable_lpf", default=False, help="ローパスフィルタを使用しません", is_flag=True, type=bool)
@click.option("--disable_hpf", default=False, help="ハイパスフィルタを使用しません", is_flag=True, type=bool)

@click.option("--print_mics", help="マイクデバイスの一覧をプリント", is_flag=True, callback=print_mics, expose_value=False, is_eager=True)

@click.option(val.ARG_NAME_VERBOSE, default=val.ARG_DEFAULT_VERBOSE, help="出力ログレベルを指定", type=click.Choice(val.ARG_CHOICE_VERBOSE))
@click.option(val.ARG_NAME_LOG_FILE, default=val.ARG_DEFAULT_LOG_FILE, help="ログファイルの出力ファイル名を指定します", type=str)
@click.option(val.ARG_NAME_LOG_DIRECTORY, default=val.ARG_DEFAULT_LOG_DIRECTORY, help="ログ格納先のディレクトリを指定します", type=str)
@click.option(val.ARG_NAME_LOG_ROTATE, default=False, help="-", is_flag=True, type=bool)
@click.option("--record",default=False, help="録音した音声をファイルとして出力します", is_flag=True, type=bool)
@click.option("--record_file", default="record", help="録音データの出力ファイル名を指定します", type=str)
@click.option("--record_directory", default=None, help="録音データの出力先ディレクトリを指定します", type=str)

@click.option("--feature", default="", help="-", type=str)
def main(
    test:str,
    method:str,
    whisper_model:str,
    whisper_device:str,
    whisper_device_index:int,
    whisper_language:str,
    google_language:str,
    google_timeout:float,
    google_convert_sampling_rate:bool,
    google_error_retry:int,
    google_duplex_parallel:bool,
    google_duplex_parallel_max:Optional[int],
    google_duplex_parallel_reduce_count:Optional[int],
    mic:Optional[int],
    mic_name:Optional[str],
    mic_api:str,

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
    mic_delay_duration:Optional[float],
    out:str,
    out_yukarinette:int,
    out_yukacone:Optional[int],
    out_illuminate:int,
    filter_lpf_cutoff:int,
    filter_lpf_cutoff_upper:int,
    filter_hpf_cutoff:int,
    filter_hpf_cutoff_upper:int,
    disable_lpf:bool,
    disable_hpf:bool,
    verbose:str,
    log_file:str,
    log_directory:Optional[str],
    log_rotate:bool,
    record:bool,
    record_file:str,
    record_directory:Optional[str],
    feature:str
    ) -> None:
    from src import ilm_logger, ilm_enviroment

    cancel = CancellationObject()
    try:
        if record_directory is None:
            record_directory = ilm_enviroment.root
        else:
            os.makedirs(record_directory, exist_ok=True)
        sampling_rate = src.mic.Mic.update_sample_rate(mic, mic_sampling_rate) #16000
        rec = Record(record, record_file, record_directory)

        # マイクにフィルタを渡すので先に用意
        filter_highPass:NoiseFilter | None = None
        filters:list[NoiseFilter] = []
        # LPFは動いてないので加えない
        #if not disable_lpf:
        #    filters.append(
        #        LowPassFilter(
        #            sampling_rate,
        #            filter_lpf_cutoff,
        #            filter_lpf_cutoff_upper))
        if not disable_hpf and filter_hpf_cutoff < filter_hpf_cutoff_upper:
            filter_highPass = HighPassFilter(
                sampling_rate,
                filter_hpf_cutoff,
                filter_hpf_cutoff_upper)
            filters.append(filter_highPass)

        ilm_logger.print("マイクの初期化")
        mp_recog_conf:recognition.RecognizeMicrophoneConfig = {
            val.METHOD_VALUE_WHISPER: lambda: recognition.WhisperMicrophoneConfig(mic_delay_duration),
            val.METHOD_VALUE_WHISPER_FASTER: lambda: recognition.WhisperMicrophoneConfig(mic_delay_duration),
            val.METHOD_VALUE_WHISPER_KOTOBA: lambda: recognition.WhisperMicrophoneConfig(mic_delay_duration),
            val.METHOD_VALUE_GOOGLE: lambda: recognition.GoogleMicrophoneConfig(mic_delay_duration),
            val.METHOD_VALUE_GOOGLE_DUPLEX: lambda: recognition.GoogleMicrophoneConfig(mic_delay_duration),
            val.METHOD_VALUE_GOOGLE_MIX: lambda: recognition.GoogleMicrophoneConfig(mic_delay_duration),
        }[method]()

        def mp_value(db, en): return db if en is None else en
        mp_energy = mp_value(db2rms(mic_db_threshold), mic_energy)
        mp_ambient_noise_to_energy = mp_value(mic_ambient_noise_to_db, mic_ambient_noise_to_energy)
        mp_dynamic_energy = mp_value(mic_dynamic_db, mic_dynamic_energy)
        mp_dynamic_energy_ratio = mp_value(mic_dynamic_db_ratio, mic_dynamic_energy_ratio)
        mp_dynamic_energy_adjustment = mp_value(mic_dynamic_db_adjustment_damping, mic_dynamic_energy_adjustment_damping)
        mp_dynamic_energy_min = mp_value(db2rms(mic_dynamic_db_min), mic_dynamic_energy_min)
        mp_mic = mic
        if mp_mic is None and (not mic_name is None) and mic_name != "":
            mp_mic = src.mic.Mic.choice_mic(mic_name, mic_api)
            if mp_mic is None:
                ilm_logger.info(f"マイク[{mic_name}]を検索しましたが見つかりませんでした", console=val.Console.Red, reset_console=True)
                ilm_logger.log("choice_mic() not found")
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
            mp_recog_conf,
            filter_highPass,
            mp_mic)
        ilm_logger.print(f"マイクは{mc.device_name}を使用します")
        ilm_logger.debug(f"#指定音圧閾値　 : {rms2db(mp_energy):.2f}", reset_console=True)
        ilm_logger.debug(f"#現在の音圧閾値 : {rms2db(mc.current_param.energy_threshold):.2f}", reset_console=True)

        if test == val.TEST_VALUE_MIC:
            main_test.run_mic(
                mc,
                rec,
                ilm_logger,
                cancel,
                feature)
        elif test == val.TEST_VALUE_AMBIENT:
            main_test.run_ambient(
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
                    device_index=whisper_device_index,
                    download_root=f"{ilm_enviroment.root}{os.sep}.cache"),
                val.METHOD_VALUE_WHISPER_KOTOBA: lambda: recognition.RecognitionModelWhisperKotoba(
                    device=whisper_device),
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
                val.METHOD_VALUE_GOOGLE_MIX: lambda: recognition.RecognitionModelGoogleMix(
                    sample_rate=sampling_rate,
                    sample_width=2,
                    convert_sample_rete=google_convert_sampling_rate,
                    language=google_language,
                    timeout=google_timeout if 0 < google_timeout else None,
                    challenge=google_error_retry,
                    parallel_max_duplex=google_duplex_parallel_max,
                    parallel_reduce_count_duplex=google_duplex_parallel_reduce_count),
            }[method]()
            ilm_logger.debug(f"#認識モデルは{type(recognition_model)}を使用", reset_console=True)

            outputer:output.RecognitionOutputer = {
                val.OUT_VALUE_PRINT: lambda: output.PrintOutputer(),
                val.OUT_VALUE_YUKARINETTE: lambda: output.YukarinetteOutputer(f"ws://localhost:{out_yukarinette}", lambda x: ilm_logger.info(x)),
                val.OUT_VALUE_YUKACONE: lambda: output.YukaconeOutputer(f"ws://localhost:{output.YukaconeOutputer.get_port(out_yukacone)}", lambda x: ilm_logger.info(x)),
                val.OUT_VALUE_ILLUMINATE: lambda: output.IlluminateSpeechOutputer(f"ws://localhost:{out_illuminate}", lambda x: ilm_logger.info(x)),
            }[out]()
            ilm_logger.debug(f"#出力は{type(outputer)}を使用", reset_console=True)
     
            ilm_logger.debug(f"#使用音声フィルタ({len(filters)}):", reset_console=True)
            for f in filters:
               ilm_logger.debug(f"#{type(f)}", reset_console=True)

            mic_ip = mc.initilaze_param
            mic_cp = mc.current_param
            # 構文警告避けassert
            assert not mic_cp.phrase_threshold is None
            assert not mic_cp.non_speaking_duration is None
            log_mic_info = os.linesep.join([
                f"initial-info",
                f"device : {mic_ip.index}",
                f"energy_threshold : {round(mic_ip.energy_threshold, 2)}",
                f"ambient_noise_to_energy : {mic_ip.ambient_noise_to_energy}",
                f"dynamic_energy : {mic_ip.dynamic_energy}",
                f"dynamic_energy_ratio : {mic_ip.dynamic_energy_ratio}",
                f"dynamic_energy_adjustment_damping : {mic_ip.dynamic_energy_ratio}",
                f"dynamic_energy_min : {mic_ip.dynamic_energy_min}",
                f"pause : {round(mic_ip.pause_threshold, 2)}",
                f"phrase : {mic_ip.phrase_threshold if mic_ip.phrase_threshold is None else round(mic_ip.phrase_threshold, 2)}",
                f"non_speaking : {mic_ip.non_speaking_duration if mic_ip.non_speaking_duration is None else round(mic_ip.non_speaking_duration, 2)}",
                "",
                "current-info",
                f"device : {mic_cp.device_name}",
                f"energy_threshold : {round(mic_cp.energy_threshold,2)}",
                f"dynamic_energy : {mic_cp.dynamic_energy}",
                f"dynamic_energy_ratio : {mic_cp.dynamic_energy_ratio}",
                f"dynamic_energy_adjustment_damping : {mic_cp.dynamic_energy_adjustment_damping}",
                f"pause : {round(mic_cp.pause_threshold, 2)}",
                f"phrase : {round(mic_cp.phrase_threshold, 2)}",
                f"non_speaking : {round(mic_cp.non_speaking_duration, 2)}",
            ])
            ilm_logger.log([
                f"マイク: {mc.device_name}",
                log_mic_info,
                f"認識モデル: {type(recognition_model)}",
                f"出力 = {type(outputer)}",
                f"フィルタ = {','.join(list(map(lambda x: f'{type(x)}', filters)))}"
            ])

            ilm_logger.print("認識中…")
            main_run.run(
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
        ilm_logger.print("ctrl+c")
    finally:
        pass
    sys.exit()


if __name__ == "__main__":
    from src import ilm_logger

    ilm_logger.log([
        "起動",
        f"platform = {platform.platform()}",
        f"python = {sys.version}",
        f"arg = {sys.argv}",
    ])

    main() # type: ignore
