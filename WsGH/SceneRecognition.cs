﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace WsGH {
	static class SceneRecognition {
		// 各種定数定義
		#region 遠征用定数
		// 遠征艦隊数
		static int ExpFleetCount = 4;
		// 遠征リストの縦幅
		static int ExpListHeight = 4;
		// 「艦隊派遣」ボタンのRect
		static RectangleF[] ExpButtonPosition = {
			new RectangleF(81.43f, 8.996f, 11.16f, 5.230f),
			new RectangleF(81.43f, 29.92f, 11.16f, 5.230f),
			new RectangleF(81.43f, 50.63f, 11.16f, 5.230f),
			new RectangleF(81.43f, 71.55f, 11.16f, 5.230f),
		};
		// 艦隊番号アイコンのRect
		static RectangleF[] ExpFleetIconPosition = {
			new RectangleF(86.60f, 6.695f, 0.9401f, 2.301f),
			new RectangleF(86.60f, 27.41f, 0.9401f, 2.301f),
			new RectangleF(86.60f, 48.33f, 0.9401f, 2.301f),
			new RectangleF(86.60f, 69.25f, 0.9401f, 2.301f),
		};
		// 艦隊番号アイコンのハッシュ値
		static ulong[] ExtFleetIconHash = {
			0x19191f1c1c1e1f1f,
			0x181ec7870e1e3f7f,
			0x381e8e0e1e07c71e,
			0xcc8e0e4ececf8f0e,
		};
		// 遠征時間表示のRect
		static float[] ExpTimerDigitPX = {60.89f, 62.63f, 65.45f, 67.10f, 69.80f, 71.56f};
		static float[] ExpTimerDigitPY = {5.858f, 26.57f, 47.49f, 68.41f};
		static float ExpTimerDigitWX = 1.645f, ExpTimerDigitWY = 4.184f;
		#endregion
		#region OCR用定数
		// OCRする際にリサイズするサイズ
		static Size TemplateSize1 = new Size(32, 32);
		// OCRする際にマッチングさせる元のサイズ
		static Size TemplateSize2 = new Size(TemplateSize1.Width + 2, TemplateSize1.Height + 2);
		// OCRする際にマッチングさせる先の画像
		static IplImage TemplateSource;
		#endregion
		#region その他定数
		static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
		#endregion

		public static void InitialSceneRecognition() {
			TemplateSource = BitmapConverter.ToIplImage(Properties.Resources.ocr_template);
		}

		/// <summary>
		/// 画像の一部分におけるDifferenceHashを取得する
		/// 座標・大きさは画像に対する％指定なことに注意
		/// </summary>
		/// <param name="bitmap">画像</param>
		/// <param name="px_per">切り取る左座標</param>
		/// <param name="py_per">切り取る上座標</param>
		/// <param name="wx_per">切り取る幅</param>
		/// <param name="wy_per">切り取る高さ</param>
		/// <returns>64bitのDifferenceHash値</returns>
		static ulong getDifferenceHash(Bitmap bitmap, double px_per, double py_per, double wx_per, double wy_per) {
			// ％指定をピクセル指定に直す
			var bitmapWidth = bitmap.Width;
			var bitmapHeight = bitmap.Height;
			var px = (int)(bitmapWidth * px_per / 100);
			var py = (int)(bitmapHeight * py_per / 100);
			var wx = (int)(bitmapWidth * wx_per / 100);
			var wy = (int)(bitmapHeight * wy_per / 100);
			// 画像を切り取り、横9ピクセル縦8ピクセルにリサイズする
			// その際にグレースケール化も同時に施す
			var canvas = new Bitmap(9, 8);
			using(var g = Graphics.FromImage(canvas)) {
				// 切り取られる位置・大きさ
				var srcRect = new Rectangle(px, py, wx, wy);
				// 貼り付ける位置・大きさ
				var desRect = new Rectangle(0, 0, canvas.Width, canvas.Height);
				// グレースケール変換用のマトリックスを設定
				var cm = new ColorMatrix(
					new float[][]{
						new float[]{0.299f, 0.299f, 0.299f, 0 ,0},
						new float[]{0.587f, 0.587f, 0.587f, 0, 0},
						new float[]{0.114f, 0.114f, 0.114f, 0, 0},
						new float[]{0, 0, 0, 1, 0},
						new float[]{0, 0, 0, 0, 1}
					}
				);
				var ia = new ImageAttributes();
				ia.SetColorMatrix(cm);
				// 描画
				// ImageAttributesを設定しなければ、
				// g.DrawImage(bitmap, desRect, srcRect, GraphicsUnit.Pixel);
				// で済むのにMSェ……
				g.DrawImage(
					bitmap, desRect, srcRect.X, srcRect.Y, 
					srcRect.Width, srcRect.Height, GraphicsUnit.Pixel, ia
				);
			}
			/*var canvas2 = new Bitmap(wx, wy);
			using(var g = Graphics.FromImage(canvas2)) {
				// 切り取られる位置・大きさ
				var srcRect = new Rectangle(px, py, wx, wy);
				// 貼り付ける位置・大きさ
				var desRect = new Rectangle(0, 0, canvas2.Width, canvas2.Height);
				// グレースケール変換用のマトリックスを設定
				var cm = new ColorMatrix(
					new float[][]{
						new float[]{0.299f, 0.299f, 0.299f, 0 ,0},
						new float[]{0.587f, 0.587f, 0.587f, 0, 0},
						new float[]{0.114f, 0.114f, 0.114f, 0, 0},
						new float[]{0, 0, 0, 1, 0},
						new float[]{0, 0, 0, 0, 1}
					}
				);
				var ia = new ImageAttributes();
				ia.SetColorMatrix(cm);
				// 描画
				// ImageAttributesを設定しなければ、
				// g.DrawImage(bitmap, desRect, srcRect, GraphicsUnit.Pixel);
				// で済むのにMSェ……
				g.DrawImage(
					bitmap, desRect, srcRect.X, srcRect.Y,
					srcRect.Width, srcRect.Height, GraphicsUnit.Pixel, ia
				);
			}
			canvas2.Save("canvas2.bmp");*/
			// 隣接ピクセルとの比較結果を符号化する
			ulong hash = 0;
			for(int y = 0; y < 8; ++y) {
				for(int x = 0; x < 8; ++x) {
					hash <<= 1;
					if(canvas.GetPixel(x, y).R > canvas.GetPixel(x + 1, y).R)
						hash |= 1;
				}
			}
			return hash;
		}
		/// <summary>
		/// 画像の一部分におけるDifferenceHashを取得する
		/// Rectは画像に対する％指定なことに注意
		/// </summary>
		/// <param name="bitmap">画像</param>
		/// <param name="rect">切り取るRect</param>
		/// <returns>64bitのDifferenceHash値</returns>
		static ulong getDifferenceHash(Bitmap bitmap, RectangleF rect) {
			return getDifferenceHash(bitmap, rect.X, rect.Y, rect.Width, rect.Height);
		}
		// ビットカウント
		static uint popcnt(ulong x) {
			x = ((x & 0xaaaaaaaaaaaaaaaa) >> 1) + (x & 0x5555555555555555);
			x = ((x & 0xcccccccccccccccc) >> 2) + (x & 0x3333333333333333);
			x = ((x & 0xf0f0f0f0f0f0f0f0) >> 4) + (x & 0x0f0f0f0f0f0f0f0f);
			x = ((x & 0xff00ff00ff00ff00) >> 8) + (x & 0x00ff00ff00ff00ff);
			x = ((x & 0xffff0000ffff0000) >> 16) + (x & 0x0000ffff0000ffff);
			x = ((x & 0xffffffff00000000) >> 32) + (x & 0x00000000ffffffff);
			return (uint)x;
		}
		// ハミング距離を計算する
		static uint getHummingDistance(ulong a, ulong b) {
			return popcnt(a ^ b);
		}
		// 周囲をトリミングする
		static Rectangle getTrimmingRectangle(Bitmap bitmap) {
			var rect = new Rectangle(new Point(0, 0), bitmap.Size);
			// 上下左右の境界を取得する
			var borderColor = Color.FromArgb(255, 255, 255);
			// 左
			for(int x = 0; x < bitmap.Width; ++x) {
				bool borderFlg = false;
				for(int y = 0; y < bitmap.Height; ++y) {
					if(bitmap.GetPixel(x, y) != borderColor) {
						borderFlg = true;
						break;
					}
				}
				if(borderFlg) {
					rect.X = x;
					rect.Width -= x;
					break;
				}
			}
			// 上
			for(int y = 0; y < bitmap.Height; ++y) {
				bool borderFlg = false;
				for(int x = 0; x < bitmap.Width; ++x) {
					if(bitmap.GetPixel(x, y) != borderColor) {
						borderFlg = true;
						break;
					}
				}
				if(borderFlg) {
					rect.Y = y;
					rect.Height -= y;
					break;
				}
			}
			// 右
			for(int x = bitmap.Width - 1; x >= 0; --x) {
				bool borderFlg = false;
				for(int y = 0; y < bitmap.Height; ++y) {
					if(bitmap.GetPixel(x, y) != borderColor) {
						borderFlg = true;
						break;
					}
				}
				if(borderFlg) {
					rect.Width -= bitmap.Width - x - 1;
					break;
				}
			}
			// 下
			for(int y = bitmap.Height - 1; y >= 0; --y) {
				bool borderFlg = false;
				for(int x = 0; x < bitmap.Width; ++x) {
					if(bitmap.GetPixel(x, y) != borderColor) {
						borderFlg = true;
						break;
					}
				}
				if(borderFlg) {
					rect.Height -= bitmap.Height - y - 1;
					break;
				}
			}
			return rect;
		}
		// 数字認識を行う
		static List<int> getDigitOCR(Bitmap bitmap, float[] px_arr, float py_per, float wx_per, float wy_per, int thresold, bool negaFlg) {
			var output = new List<int>();
			foreach(var px_per in px_arr) {
				// ％指定をピクセル指定に直す
				var bitmapWidth = bitmap.Width;
				var bitmapHeight = bitmap.Height;
				var px = (int)(bitmapWidth * px_per / 100);
				var py = (int)(bitmapHeight * py_per / 100);
				var wx = (int)(bitmapWidth * wx_per / 100);
				var wy = (int)(bitmapHeight * wy_per / 100);
				// 画像を切り取る
				var canvas = new Bitmap(wx, wy);
				using(var g = Graphics.FromImage(canvas)) {
					// 切り取られる位置・大きさ
					var srcRect = new Rectangle(px, py, wx, wy);
					// 貼り付ける位置・大きさ
					var desRect = new Rectangle(0, 0, canvas.Width, canvas.Height);
					g.DrawImage(bitmap, desRect, srcRect, GraphicsUnit.Pixel);
				}
				//canvas.Save("digit1.bmp");
				// 二値化する
				using(var image = BitmapConverter.ToIplImage(canvas))
				using(var image2 = new IplImage(image.Size, BitDepth.U8, 1)) {
					Cv.CvtColor(image, image2, ColorConversion.BgrToGray);
					if(negaFlg)
						Cv.Not(image2, image2);
					Cv.Threshold(image2, image2, thresold, 255, ThresholdType.Binary);
					canvas = image2.ToBitmap();
				}
				//canvas.Save("digit2.bmp");
				// 周囲をトリミングした上で、所定のサイズにリサイズする
				// 背景は赤色に塗りつぶすこと
				var rect = getTrimmingRectangle(canvas);
				var canvas2 = new Bitmap(TemplateSize2.Width, TemplateSize2.Height);
				using(var g = Graphics.FromImage(canvas2)) {
					// 事前にcanvas2を赤色に塗りつぶす
					g.FillRectangle(Brushes.Red, 0, 0, canvas2.Width, canvas2.Height);
					// 切り取られる位置・大きさ
					var srcRect = rect;
					// 貼り付ける位置・大きさ
					var desRect = new Rectangle(1, 1, TemplateSize1.Width, TemplateSize1.Height);
					g.DrawImage(canvas, desRect, srcRect, GraphicsUnit.Pixel);
				}
				//canvas2.Save("digit3.bmp");
				// マッチングを行う
				Point matchPosition;
				using(var image = BitmapConverter.ToIplImage(canvas2)) {
					var resultSize = new CvSize(TemplateSource.Width - image.Width + 1, TemplateSource.Height - image.Height + 1);
					using(var resultImage = Cv.CreateImage(resultSize, BitDepth.F32, 1)) {
						Cv.MatchTemplate(TemplateSource, image, resultImage, MatchTemplateMethod.SqDiff);
						CvPoint minPosition, maxPosition;
						Cv.MinMaxLoc(resultImage, out minPosition, out maxPosition);
						matchPosition = new Point(minPosition.X, minPosition.Y);
					}
				}
				// マッチング結果を数値に翻訳する
				int matchNumber = (int)Math.Round(1.0 * matchPosition.X / TemplateSize2.Width / 2, 0);
				matchNumber = (matchNumber < 0 ? 0 : matchNumber > 10 ? 10 : matchNumber);
				output.Add(matchNumber);
			}
			return output;
		}
		// 時刻を正規化する
		static uint getLeastSecond(List<int> timerDigit) {
			/*foreach(var d in timerDigit)
				Console.Write(d + " ");
			Console.WriteLine("");*/
			timerDigit[0] = (timerDigit[0] > 5 ? 0 : timerDigit[0]);
			timerDigit[1] = (timerDigit[1] > 9 ? 0 : timerDigit[1]);
			timerDigit[2] = (timerDigit[2] > 5 ? 0 : timerDigit[2]);
			timerDigit[3] = (timerDigit[3] > 9 ? 0 : timerDigit[3]);
			timerDigit[4] = (timerDigit[4] > 5 ? 0 : timerDigit[4]);
			timerDigit[5] = (timerDigit[5] > 9 ? 0 : timerDigit[5]);
			var hour = timerDigit[0] * 10 + timerDigit[1];
			var minute = timerDigit[2] * 10 + timerDigit[3];
			var second = timerDigit[4] * 10 + timerDigit[5];
			//Console.WriteLine(hour + ":" + minute + ":" + second);
			return (uint)((hour * 60 + minute) * 60 + second);
		}
		// UNIX時間を計算する
		public static ulong GetUnixTime(DateTime dt) {
			var dt2 = dt.ToUniversalTime();
			var elapsedTime = dt2 - UnixEpoch;
			return (ulong)elapsedTime.TotalSeconds;
		}
		// 遠征のシーンかを判定する
		public static bool isExpeditionScene(Bitmap bitmap) {
			{
				var hash = getDifferenceHash(bitmap, 10.11, 76.36, 3.525, 6.276);
				if(getHummingDistance(hash, 0x2d2e2ba5aaa22a2a) >= 20)
					return false;
			}
			{
				var hash = getDifferenceHash(bitmap, 21.86, 62.76, 3.878, 1.883);
				if(getHummingDistance(hash, 0xd2d0b8a8a4545656) >= 20)
					return false;
			}
			return true;
		}
		// 遠征タイマーを取得する
		public static Dictionary<int, ulong> getExpeditionTimer(Bitmap bitmap) {
			var output = new Dictionary<int, ulong>();
			var now_time = GetUnixTime(DateTime.Now);
			for(int li = 0; li < ExpListHeight; ++li) {
				// 「艦隊派遣」ボタンが出ていれば、その行に遠征艦隊はいない
				var bhash = getDifferenceHash(bitmap, ExpButtonPosition[li]);
				if(getHummingDistance(bhash, 0xd3d2dcd648b4a638) < 20)
					continue;
				// 遠征している艦隊の番号を取得する
				// ハッシュに対するハミング距離を計算した後、LINQで最小値のインデックス(艦隊番号)を取り出す
				var fhash = getDifferenceHash(bitmap, ExpFleetIconPosition[li]);
				var hd = new List<uint>();
				for(int fi = 0; fi < ExpFleetCount; ++fi) {
					hd.Add(getHummingDistance(fhash, ExtFleetIconHash[fi]));
				}
				int fleetIndex = hd
					.Select((val, idx) => new { V = val, I = idx })
					.Aggregate((min, working) => (min.V < working.V) ? min : working)
					.I;
				// 遠征時間を取得する
				//Console.WriteLine((li + 1) + "番目：第" + (fleetIndex + 1) + "艦隊");
				// 遠征完了時間を計算して書き込む
				//bitmap.Save("ss.png");
				var timerDigit = getDigitOCR(bitmap, ExpTimerDigitPX, ExpTimerDigitPY[li], ExpTimerDigitWX, ExpTimerDigitWY, 140, true);
				var leastSecond = getLeastSecond(timerDigit);
				output[fleetIndex] = now_time + leastSecond;
			}
			return output;
		}
	}
}
