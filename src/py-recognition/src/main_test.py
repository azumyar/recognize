import os
import sys
import platform
import traceback
import audioop
import numpy as np
import datetime as dt
from concurrent.futures import ThreadPoolExecutor
from typing import Any, Callable, Iterable, Optional, NamedTuple


from src import Logger, db2rms, rms2db
import src.mic
import src.val as val
import src.exception
from src.main_common import Record, save_wav
from src.cancellation import CancellationObject

def run_mic(mic:src.mic.Mic, rec:Record, logger:Logger, cancel:CancellationObject, _:str) -> None:
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
            logger.print(f"入力　　　 : {rms2db(param.energy.value):.2f}dB")
            logger.print(f"現在の閾値 : {rms2db(pp.energy_threshold):.2f}dB")
        logger.print(" ")

        logger.log([
            f"認識 #{str(index).zfill(2)}",
            f"PCM    = {round(sec - mic.end_insert_sec, 2)}sec",
            f"dB     = {(rms2db(param.energy.value) if not param.energy is None else 0):.2f}, threshold={rms2db(pp.energy_threshold):.2f}",
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
        f"current energy_threshold = {prm.energy_threshold}"
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


def run_ambient(
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