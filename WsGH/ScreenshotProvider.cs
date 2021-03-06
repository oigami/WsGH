﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Windows;

namespace WsGH {
	delegate void AfterAction();
	class ScreenshotProvider {
		Rectangle screenshotRectangle = new Rectangle(0, 0, 0, 0);
		Rectangle virtualDisplayRectangle;
		int clickPointX, clickPointY;
		Color backgroundColor;
		public ScreenshotProvider(AfterAction aa, Color backgroundColor) {
			this.backgroundColor = backgroundColor;
			virtualDisplayRectangle = calcVirtualDisplayRectangle();
			var cw = new ClickWindow(this, virtualDisplayRectangle, aa);
			cw.Show();
		}
		// 全てのディスプレイを覆う仮想スクリーンの位置および大きさを計算
		Rectangle calcVirtualDisplayRectangle() {
			// 仮想スクリーンの位置および大きさを、1枚目のディスプレイで初期化
			Rectangle vdr = Screen.AllScreens.First().Bounds;
			// 各ディスプレイについて計算を行う
			foreach(var screen in Screen.AllScreens) {
				// X1・Y1は1枚付け足した際の左端・上端(スクリーン座標)
				var X1 = Math.Min(vdr.Left, screen.Bounds.Left);
				var Y1 = Math.Min(vdr.Top, screen.Bounds.Top);
				// X2・Y2は1枚付け足した際の右端・下端(スクリーン座標)
				var X2 = Math.Max(vdr.Right, screen.Bounds.Right);
				var Y2 = Math.Max(vdr.Bottom, screen.Bounds.Bottom);
				// 計算結果を変数vdrに反映し直す
				vdr.X = X1;
				vdr.Y = Y1;
				vdr.Width = X2 - X1;
				vdr.Height = Y2 - Y1;
			}
			return vdr;
		}
		// 仮想スクリーンのビットマップを生成する
		Bitmap getVirtualDisplayBitmap(Rectangle virtualDisplayRectangle) {
			var vdb = new Bitmap(virtualDisplayRectangle.Width, virtualDisplayRectangle.Height);
			using(var g = Graphics.FromImage(vdb)) {
				g.CopyFromScreen(virtualDisplayRectangle.Location, new System.Drawing.Point(), vdb.Size);
			}
			return vdb;
		}
		// 座標取得に成功したかを判定する
		public bool isGetPosition() {
			if(screenshotRectangle.Width <= 0)
				return false;
			if(screenshotRectangle.Height <= 0)
				return false;
			return true;
		}
		// 取得した座標を表示する
		public string getPositionStr() {
			return "(" + screenshotRectangle.X.ToString() + "," + screenshotRectangle.Y.ToString() + ") " + screenshotRectangle.Width.ToString() + "x" + screenshotRectangle.Height.ToString();
		}
		// スクショを取得する(スクショ出来ない際はnullが返る)
		public Bitmap getScreenShot(bool forTwitterFlg) {
			if(!isGetPosition())
				return null;
			var bitmap = new Bitmap(screenshotRectangle.Width, screenshotRectangle.Height);
			using(var g = Graphics.FromImage(bitmap)) {
				g.CopyFromScreen(screenshotRectangle.Location, new System.Drawing.Point(), bitmap.Size);
			}
			if(forTwitterFlg) {
				// Twitter用に画像を少しだけ加工する
				var color = bitmap.GetPixel(0, 0);
				var color_a = (color.A == 0 ? 1 : color.A - 1);
				var color_r = color.R;
				var color_g = color.G;
				var color_b = color.B;
				bitmap.SetPixel(0, 0, Color.FromArgb(color_a, color_r, color_g, color_b));
			}
			return bitmap;
		}
		// ズレを検知する
		public bool IsPositionShifting() {
			// 通常より1ピクセルだけ大きく取得する
			var bitmap = new Bitmap(screenshotRectangle.Width + 2, screenshotRectangle.Height + 2);
			using(var g = Graphics.FromImage(bitmap)) {
				g.CopyFromScreen(screenshotRectangle.X - 1, screenshotRectangle.Y - 1, 0, 0, bitmap.Size);
			}
			var backgroundColorArgb = backgroundColor.ToArgb();
			if((bitmap.GetPixel(0, 0).ToArgb() != backgroundColorArgb)
			&& (bitmap.GetPixel(0, screenshotRectangle.Height + 1).ToArgb() != backgroundColorArgb)
			&& (bitmap.GetPixel(screenshotRectangle.Width + 1, 0).ToArgb() != backgroundColorArgb)
			&& (bitmap.GetPixel(screenshotRectangle.Width + 1, screenshotRectangle.Height + 1).ToArgb() != backgroundColorArgb)) {
				return true;
			}
			return false;
		}
		// ズレを自動修正する
		public bool TryPositionShifting() {
			var vdb = getVirtualDisplayBitmap(virtualDisplayRectangle);
			// クリックした座標から、ゲーム画面の座標を逆算する
			var gameWindowRectangle = getGameWindowRectangle(vdb, backgroundColor, clickPointX, clickPointY);
			// ゲーム画面の座標をスクリーン座標に変換する
			screenshotRectangle.X = gameWindowRectangle.X + virtualDisplayRectangle.X;
			screenshotRectangle.Y = gameWindowRectangle.Y + virtualDisplayRectangle.Y;
			screenshotRectangle.Size = gameWindowRectangle.Size;
			// サイズ判定
			if(!isGetPosition())
				return false;
			// ズレ判定
			if(IsPositionShifting())
				return false;
			return true;
		}
		// ゲーム画面の座標を逆算する
		static Rectangle getGameWindowRectangle(Bitmap bitmap, Color backgroundColor, int clickPointX, int clickPointY) {
			var gwr = new Rectangle(clickPointX, clickPointY, 0, 0);
			// 上下左右の境界を取得する
			var borderColor = Color.FromArgb(backgroundColor.R, backgroundColor.G, backgroundColor.B);
			const int borderDiff = 5;
			// 左
			for(int x = clickPointX - 1; x >= 0; --x) {
				if(bitmap.GetPixel(x, clickPointY) != borderColor)
					continue;
				if(bitmap.GetPixel(x, clickPointY - borderDiff) != borderColor)
					continue;
				if(bitmap.GetPixel(x, clickPointY + borderDiff) != borderColor)
					continue;
				gwr.X = x + 1;
				break;
			}
			// 上
			for(int y = clickPointY - 1; y >= 0; --y) {
				if(bitmap.GetPixel(clickPointX, y) != borderColor)
					continue;
				if(bitmap.GetPixel(clickPointX - borderDiff, y) != borderColor)
					continue;
				if(bitmap.GetPixel(clickPointX + borderDiff, y) != borderColor)
					continue;
				gwr.Y = y + 1;
				break;
			}
			// 右
			for(int x = clickPointX + 1; x < bitmap.Width; ++x) {
				if(bitmap.GetPixel(x, clickPointY) != borderColor)
					continue;
				if(bitmap.GetPixel(x, clickPointY - borderDiff) != borderColor)
					continue;
				if(bitmap.GetPixel(x, clickPointY + borderDiff) != borderColor)
					continue;
				gwr.Width = x - gwr.X;
				break;
			}
			// 下
			for(int y = clickPointY + 1; y < bitmap.Height; ++y) {
				if(bitmap.GetPixel(clickPointX, y) != borderColor)
					continue;
				if(bitmap.GetPixel(clickPointX - borderDiff, y) != borderColor)
					continue;
				if(bitmap.GetPixel(clickPointX + borderDiff, y) != borderColor)
					continue;
				gwr.Height = y - gwr.Y;
				break;
			}
			return gwr;
		}
		// ゲーム画面の座標をクリックさせるためのインナークラス
		sealed class ClickWindow : Window {
			// (thisでScreenshotProvider内の変数・メソッドを叩かせているので仕方ないね)
			ScreenshotProvider ScreenshotProvider;
			// 表示用ビットマップ
			Bitmap vdb;
			// 表示する座標
			Rectangle vdr;
			// クリック完了時に何とかするための奴
			AfterAction aa;
			// BitmapSourceを引っ張るためにコレを使わざるを得ない現実
			internal static class NativeMethods {
				[System.Runtime.InteropServices.DllImport("gdi32.dll")]
				public static extern bool DeleteObject(IntPtr hObject);
			}
			// コンストラクタ
			public ClickWindow(ScreenshotProvider sp, Rectangle virtualDisplayRectangle, AfterAction aa) {
				// 仕方ないね
				ScreenshotProvider = sp;
				vdr = virtualDisplayRectangle;
				// クリック完了時にGUIに反映するための細工
				this.aa = aa;
				// 表示用に仮想スクリーンのビットマップを生成する
				vdb = sp.getVirtualDisplayBitmap(vdr);
				// 画像を表示するためImageコントロールを用意する
				// (http://bacchus.ivory.ne.jp/gin/post-979/)
				var ScreenshotImage = new System.Windows.Controls.Image();
				var hbitmap = vdb.GetHbitmap();
				ScreenshotImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
					hbitmap, IntPtr.Zero, Int32Rect.Empty,
					System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
				NativeMethods.DeleteObject(hbitmap);
				ScreenshotImage.Height = vdr.Height;
				ScreenshotImage.Width = vdr.Width;
				// Gridを作成し、Imageコントロールを乗せる
				var grid = new System.Windows.Controls.Grid();
				grid.Children.Add(ScreenshotImage);
				// ウィンドウについての設定を行う
				// 縁無しに設定する
				// (http://5000164.jp/2014-03-wpf_practice_1/)
				WindowStyle = WindowStyle.None;
				AllowsTransparency = true;
				// 表示座標を決める
				Left = virtualDisplayRectangle.Left;
				Top = virtualDisplayRectangle.Top;
				Height = virtualDisplayRectangle.Height;
				Width = virtualDisplayRectangle.Width;
				// コンテンツを設定する
				Content = grid;
				// イベントを設定する
				MouseDown += ClickWindow_Click;
				KeyDown += ClickWindow_Key;
			}
			// クリックイベント
			private void ClickWindow_Click(object sender, RoutedEventArgs e) {
				// クリックした座標を、クライアント座標基準で取得する
				ScreenshotProvider.clickPointX = Control.MousePosition.X - vdr.Left;
				ScreenshotProvider.clickPointY = Control.MousePosition.Y - vdr.Top;
				// クリックした座標から、ゲーム画面の座標を逆算する
				var gameWindowRectangle = getGameWindowRectangle(vdb, ScreenshotProvider.backgroundColor, ScreenshotProvider.clickPointX, ScreenshotProvider.clickPointY);
				// ゲーム画面の座標をスクリーン座標に変換する
				ScreenshotProvider.screenshotRectangle.X = gameWindowRectangle.X + vdr.X;
				ScreenshotProvider.screenshotRectangle.Y = gameWindowRectangle.Y + vdr.Y;
				ScreenshotProvider.screenshotRectangle.Size = gameWindowRectangle.Size;
				// GUIにアクションを伝達させる
				aa();
				Close();
			}
			// キーボードイベント
			private void ClickWindow_Key(object sender, System.Windows.Input.KeyEventArgs e) {
				// Escキーなら反応する
				if(e.Key == System.Windows.Input.Key.Escape) {
					aa();
					Close();
				}
			}
		}
	}
}
