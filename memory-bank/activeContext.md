# 褰撳墠涓婁笅鏂?
鐗堟湰: v0.5.3

- 20260328: 鏈疆鑱氱劍 AspenBurner 绋冲畾鎬у拰璁剧疆浣撻獙鍥炲綊銆?
- 20260328: 宸插姞杩愯鏃跺紓甯搁槻鎶わ紝`OverlayRuntime` 鐨?state tick 鍑洪敊鍙鏃ュ織骞堕檷绾э紝涓嶅啀鐩存帴鎵撴杩涚▼銆?
- 20260328: 宸茬粰 WinForms 杩涚▼鎸傚叏灞€寮傚父鏃ュ織锛屽悗缁穿婧冨彲鍦?`logs/aspenburner-*.log` 杩芥爤銆?
- 20260328: 宸蹭慨璁剧疆闈㈡澘閰嶇疆搴旂敤閾捐矾锛屾敼鍔ㄩ厤缃細绔嬪嵆椹卞姩鐪熷疄 overlay 鍒锋柊锛屼笉鍐嶅彧鏀归瑙堛€?
- 20260328: 宸茶ˉ `Reset`銆佹帹鑽愰璁俱€佸皬缁垮崄瀛?灏忛粍鍗佸瓧/榛?T 瀛楋紝骞朵繚鐣?CPU 瑙掓爣璁剧疆銆?
- 20260328: 宸茶ˉ绐椾綋鍒濆鍖栧洖褰掓祴璇曪紝淇帀棰勮璇存槑鏍囩鍒濆鍖栭『搴忓鑷寸殑 NRE銆?
- 20260328: 涓嬩竴姝ヨ嫢鐢ㄦ埛缁х画鍙嶉鈥滀繚瀛樹笉鐢熸晥鈥濓紝鍏堟煡鏄惁浠嶅湪浣跨敤鏃х鐞嗗憳瀹炰緥鎴栨棫蹇嵎鏂瑰紡銆?

- 20260328: BIOS/EC research: matched Clevo NPxxSNx(-G) baseline, downloaded B10723 + EC10708, confirmed B10724 listing exists but source file is missing.

- 20260329: Firmware rollback status: BIOS now 1.07.23 and EC 7.08, but Event 37 persists and Win32_Processor still reports 2100/2100 MHz; root cause not resolved by firmware version rollback alone.

- 20260329: 新增 CPU 验证工具 AspenBurner.Bench，已完成主线程帧循环、多核吞吐、遥测采样、Event 37 探针与文本报告。
- 20260329: Bench 正式实测 75s，AverageFrequencyMHz=3534、MaxFrequencyMHz=4518、PeakTemperatureC=87、Event37Delta=0，结论为‘正常’。


- 20260329: Bench 75s on Performance + custom fan + dGPU only => AvgFreq=3931MHz, PeakTemp=98C, Event37Delta=0, conclusion=normal.

- 20260329: Bench 75s with external intake fan under Performance + custom fan + dGPU only => AvgFreq=3907MHz, PeakTemp=98C, P95=3.85ms, Parallel=904628396 ops/s, Event37Delta=0, conclusion=normal.

- 20260329: Real-game session monitor (~10.5 min, Performance + Maximum) => Event37Delta=0, combat avg CPU freq=3812MHz, combat avg CPU temp=97.79C, combat avg GPU util=66.07%, combat avg GPU temp=78.83C; no PresentMon CSV, so direct FPS capture failed.

- 20260329: AspenBurner.Bench telemetry extended with AverageTemperatureC; targeted tests passed and local short-run bench now reports avg+peak temp together.
- 20260329: Local skill C:\Users\Aspen\.codex\skills\clevo-gaming-thermal-tuning\ created and verified; includes CC40 profile switch, Windows power-plan switch, and bench runner scripts.
- 20260329: Verified skill path works on this machine via RadioButton AutomationIds Btn_entertainment, RB_FAN_custom, 2; current recommended default game path is HighPerformance + Entertainment + Custom.