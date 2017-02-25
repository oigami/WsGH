﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WsGH {
	using SupplyPair = KeyValuePair<DateTime, int>;
	using SupplyList = List<KeyValuePair<DateTime, int>>;
	using System.Drawing;
	static class SupplyStore {
		#region MainSupply関係
		#region メンバ変数
		// MainSupplyの最終更新日時
		static DateTime lastUpdate = new DateTime();
		// MainSupplyの本体
		public static List<SupplyData> MainSupplyData = null;
		// MainSupplyの大きさ
		public static int MainSupplyTypeCount;
		public static int MainSupplyListCount = 0;
		// MainSupplyの更新間隔
		static double MainSupplyIntervalMinute = 60.0;
		#endregion
		// MainSupplyの初期化
		static void InitialMainSupply() {
			MainSupplyData = new List<SupplyData> {
				new SupplyData("Fuel", Color.Green),
				new SupplyData("Ammo", Color.Chocolate),
				new SupplyData("Steel", Color.DarkGray),
				new SupplyData("Bauxite", Color.OrangeRed),
				new SupplyData("Diamond", Color.SkyBlue),
			};
			MainSupplyTypeCount = MainSupplyData.Count;
			MainSupplyListCount = 0;
		}
		// MainSupplyをCSVから読み込む
		public static void ReadMainSupply() {
			// CSVから読み込む
			var lastUpdate = new DateTime();
			InitialMainSupply();
			using(var sr = new System.IO.StreamReader(@"MainSupply.csv")) {
				while(!sr.EndOfStream) {
					// 1行を読み込む
					var line = sr.ReadLine();
					// マッチさせてから各数値を取り出す
					var pattern = @"(?<Year>\d+)/(?<Month>\d+)/(?<Day>\d+) (?<Hour>\d+):(?<Minute>\d+):(?<Second>\d+),(?<Fuel>\d+),(?<Ammo>\d+),(?<Steel>\d+),(?<Bauxite>\d+),(?<Diamond>\d+)";
					var match = Regex.Match(line, pattern);
					if(!match.Success) {
						continue;
					}
					// 取り出した数値を元に、MainSupplyDataに入力する
					try {
						// 読み取り
						var supplyDateTime = new DateTime(
							int.Parse(match.Groups["Year"].Value),
							int.Parse(match.Groups["Month"].Value),
							int.Parse(match.Groups["Day"].Value),
							int.Parse(match.Groups["Hour"].Value),
							int.Parse(match.Groups["Minute"].Value),
							int.Parse(match.Groups["Second"].Value));
						int[] supplyData = {
							int.Parse(match.Groups["Fuel"].Value),
							int.Parse(match.Groups["Ammo"].Value),
							int.Parse(match.Groups["Steel"].Value),
							int.Parse(match.Groups["Bauxite"].Value),
							int.Parse(match.Groups["Diamond"].Value)};
						// データベースに入力
						for(int ti = 0; ti < MainSupplyTypeCount; ++ti) {
							MainSupplyData[ti].List.Add(new SupplyPair(supplyDateTime, supplyData[ti]));
						}
						if(lastUpdate < supplyDateTime) {
							lastUpdate = supplyDateTime;
						}
					} catch(Exception){
						InitialMainSupply();
						throw new Exception();
					}
				}
			}
			foreach(var supplyData in MainSupplyData){
				supplyData.List.Sort((a, b) => (a.Key < b.Key ? -1 : 1));
			}
			MainSupplyListCount = MainSupplyData.First().List.Count;
			// 最終更新日時を更新
			SupplyStore.lastUpdate = lastUpdate;
		}
		// MainSupplyに追記できるかを判定する
		// (MainSupplyIntervalMinute分以上開けないと追記できない設定とした)
		public static bool CanAddMainSupply() {
			var nowTime = DateTime.Now;
			return ((nowTime - lastUpdate).TotalMinutes >= MainSupplyIntervalMinute);
		}
		// MainSupplyに追記する
		public static void AddMainSupply(DateTime supplyDateTime, List<int> supply) {
			// データを書き込み
			for(int ti = 0; ti < MainSupplyTypeCount; ++ti) {
				MainSupplyData[ti].List.Add(new SupplyPair(supplyDateTime, supply[ti]));
			}
			++MainSupplyListCount;
			// 最終更新日時を更新
			lastUpdate = supplyDateTime;
		}
		// MainSupplyを表示する
		public static void ShowMainSupply() {
			Console.WriteLine("資材ログ：");
			for(int li = 0; li < MainSupplyListCount; ++li) {
				Console.Write($"{MainSupplyData.First().List[li].Key}");
				for(int ti = 0; ti < MainSupplyTypeCount; ++ti) {
					Console.Write($",{MainSupplyData[ti].List[li].Value}");
				}
				Console.WriteLine("");
			}
		}
		// MainSupplyをCSVに保存する
		public static void SaveMainSupply() {
			using(var sw = new System.IO.StreamWriter(@"MainSupply.csv")) {
				sw.WriteLine("時刻,燃料,弾薬,鋼材,ボーキサイト,ダイヤ");
				for(int li = 0; li < MainSupplyListCount; ++li) {
					sw.Write($"{MainSupplyData.First().List[li].Key}");
					for(int ti = 0; ti < MainSupplyTypeCount; ++ti) {
						sw.Write($",{MainSupplyData[ti].List[li].Value}");
					}
					sw.WriteLine("");
				}
			}
		}
		#endregion
		#region SubSupply関係

		#endregion
		#region 内部クラス
		public class SupplyData {
			// 時系列データ
			public SupplyList List;
			// 種別
			public string Type;
			// グラフの描画色
			public Color Color;
			// コンストラクタ
			public SupplyData(string type, Color color) {
				List = new SupplyList();
				Type = type;
				Color = color;
			}
		}
		#endregion
	}
}
