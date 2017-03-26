using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs; //CommonOpenFileDialog
using System;
using System.Windows;


namespace MosaicImageMaker
{
    public class ImgPath
    {
        private string[] asSrcImg;
        private string sSrcDir;
        private string sTgtImg;
        private string sDstImg;

        public string[] AsSrcImg
        {
            get
            {
                return asSrcImg;
            }

            set
            {
                asSrcImg = value;
            }
        }

        public string SSrcDir
        {
            get
            {
                return sSrcDir;
            }

            set
            {
                sSrcDir = value;
            }
        }

        public string STgtImg
        {
            get
            {
                return sTgtImg;
            }

            set
            {
                sTgtImg = value;
            }
        }

        public string SDstImg
        {
            get
            {
                return sDstImg;
            }

            set
            {
                sDstImg = value;
            }
        }
    }

    // 通知内容
    public class ProgressInfo
    {
        public ProgressInfo(int value, string message)
        {
            Value = value;
            Message = message;
        }

        public int Value { get; private set; }
        public string Message { get; private set; }
    }

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        InifileUtils m_iniUtl = null;
        ImgPath m_path = null;
        bool m_bUseSubDir;

        internal InifileUtils IniUtl
        {
            get
            {
                return m_iniUtl;
            }

            set
            {
                m_iniUtl = value;
            }
        }

        public ImgPath Path
        {
            get
            {
                return m_path;
            }

            set
            {
                m_path = value;
            }
        }

        public bool BUseSubDir
        {
            get
            {
                return m_bUseSubDir;
            }

            set
            {
                m_bUseSubDir = value;
            }
        }

        enum SG { set, get };

        public MainWindow()
        {
            InitializeComponent();

            IniUtl = new InifileUtils();
            Path = new ImgPath();

            UpdateIni(SG.get);
            UpdateWindow(SG.set);
        }

        ~MainWindow()
        {
            UpdateIni(SG.set);
        }


        private void UpdateIni(SG sg)
        {
            if (sg==SG.set)
            {
                IniUtl.setValue("Path", "TgtImg", Path.STgtImg);
                IniUtl.setValue("Path", "SrcDir", Path.SSrcDir);
                IniUtl.setValue("Path", "DstImg", Path.SDstImg);
                IniUtl.setValue("Check", "SubDir", BUseSubDir ? 1 : 0);
            }
            else
            {
                Path.STgtImg = IniUtl.getValueString("Path", "TgtImg");
                Path.SSrcDir = IniUtl.getValueString("Path", "SrcDir");
                Path.SDstImg = IniUtl.getValueString("Path", "DstImg");
                BUseSubDir = (IniUtl.getValueInt("Check", "SubDir") == 0) ? false : true;
            }
        }

        private void UpdateWindow(SG sg)
        {
            if (sg == SG.set)
            {
                if (Path.STgtImg != "") { TextBox_TgtImgDir.Text = Path.STgtImg; }
                if (Path.SSrcDir != "") { TextBox_SecImgDir.Text = Path.SSrcDir; }
                if (Path.SDstImg != "") { TextBox_DstImgDir.Text = Path.SDstImg; }
                CheckBox_SubDir.IsChecked = BUseSubDir;
            }
            else
            {
                Path.STgtImg = TextBox_TgtImgDir.Text;
                Path.SSrcDir = TextBox_SecImgDir.Text;
                Path.SDstImg = TextBox_DstImgDir.Text;
                BUseSubDir = (bool)CheckBox_SubDir.IsChecked;
            }

            if (Path.SSrcDir != "")
            {
                UpdateWindow_SecImgDir();
            }
        }

        private void UpdateWindow_SecImgDir()
        {
            try
            {
                if (BUseSubDir) {
                    Path.AsSrcImg = System.IO.Directory.GetFiles(
                        Path.SSrcDir, "*.jpg", System.IO.SearchOption.AllDirectories);
                }
                else
                {
                    Path.AsSrcImg = System.IO.Directory.GetFiles(
                        Path.SSrcDir, "*.jpg", System.IO.SearchOption.TopDirectoryOnly);
                }
                TextBox_SecImgDir.Text = Path.SSrcDir;

                if(Path.AsSrcImg.Length>= DEF.LET_IMG_MIN)
                {
                    TextBlock_DrcImgCnt.Text = Path.AsSrcImg.Length.ToString("#,0") + "[枚] の画像が使えるっぽい。";
                }
                else
                {
                    TextBlock_DrcImgCnt.Text = Path.AsSrcImg.Length.ToString("#,0") + "[枚] 画像があるけど..."
                        + DEF.LET_IMG_MIN + "枚以上は画像欲しいかも？" ;
                }
            }
            catch (Exception)
            {
                TextBlock_DrcImgCnt.Text = "";
            }
        }


        private async void Button_Execute_Click(object sender, RoutedEventArgs e)
        {
            UpdateWindow(SG.get);
            UpdateIni(SG.set);

            //  出力先にファイル出力可能か確認
            try
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter(
                    Path.SDstImg, false, System.Text.Encoding.Unicode);
                //TextBox1.Textの内容を書き込む
                sw.Write("Test");
                //閉じる
                sw.Close();
            }
            catch(Exception)
            {
                MessageBox.Show("コレ、書けないかも↓↓\n" + Path.SDstImg, "ムリぽ" );
                return;
            }


            // Progressクラスのインスタンスを生成
            var spProg1 = new Progress<int>(ShowProgress1);
            var spProg2 = new Progress<int>(ShowProgress2);

            progressBar1.Maximum = Path.AsSrcImg.Length;
            progressBar1.Value = 0;

            progressBar2.Maximum = DEF.PERCENT_MAX;
            progressBar2.Value = 0;


            //  実処理実行
            CoreResult coreResult = new CoreResult();
            MosImgCore.ECode edRet = await MosImgCore.Do(Path, coreResult, spProg1, spProg2);


            if (edRet >= MosImgCore.ECode.Success)
            {
                TextBlock_Report.Text = "できたよー (⌒∇⌒) : 平均残差 = " + (int)Math.Sqrt(coreResult.dDeltaAve) + ", 最大残差 = " + (int)Math.Sqrt(coreResult.dDeltaMax);
            }
            else
            {
                TextBlock_Report.Text = "なんか失敗したっぽい。";
                switch (edRet)
                {
                    case MosImgCore.ECode.Er_ReadTgeImg:
                        MessageBox.Show("コレ、読めないかも↓↓\n" + Path.STgtImg,"ムリぽ");
                        break;
                    case MosImgCore.ECode.Er_LackSrcImg:
                        MessageBox.Show("素材画像を確認したけど、実際に使える画像が少なすぎるかも...","ムリぽ");
                        break;
                    default:
                        break;
                }
            }

            GC.Collect();
        }

        /////////////////////////////////////////////////////////////////////////
        // // 進捗を表示するメソッド（これはUIスレッドで呼び出される）
        /////////////////////////////////////////////////////////////////////////
        private void ShowProgress1(int iVal)
        {
            progressBar1.Value = iVal;
            TextBlock_Report.Text = "まずは一通り素材画像を見てみるー : " + iVal.ToString() + "/" + progressBar1.Maximum + " [枚]";
        }
        private void ShowProgress2(int iVal)
        {
            progressBar2.Value = iVal;
            TextBlock_Report.Text = "タイリングの組み合わせを考えてるー : " + iVal.ToString() + "/" + progressBar2.Maximum + " [％]";
        }



        /////////////////////////////////////////////////////////////////////////
        // パスの選択ボタン
        /////////////////////////////////////////////////////////////////////////

        private void SetTgtPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "目標画像ファイルを開く";
            dialog.Filter = "JPEGファイル(*.jpg)|*.jpg";
            if (System.IO.Path.IsPathRooted(Path.STgtImg))
            {
                dialog.InitialDirectory = System.IO.Path.GetDirectoryName(Path.STgtImg);
            }
            if (dialog.ShowDialog() == true)
            {
                Path.STgtImg = dialog.FileName;
                UpdateWindow(SG.set);
            }
        }

        private void SetSrcDir_Click(object sender, RoutedEventArgs e)
        {
            var Dialog = new CommonOpenFileDialog();
            // フォルダーを開く設定に
            Dialog.IsFolderPicker = true;
            // 読み取り専用フォルダ/コントロールパネルは開かない
            Dialog.EnsureReadOnly = false;
            Dialog.AllowNonFileSystemItems = false;
            // パス指定
            Dialog.DefaultDirectory = Path.SSrcDir;
            // 開く
            var Result = Dialog.ShowDialog();
            // もし開かれているなら
            if (Result == CommonFileDialogResult.Ok)
            {
                Path.SSrcDir = Dialog.FileName;
                UpdateWindow(SG.set);
            }
        }

        private void SetDstPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "主力画像ファイルを保存";
            dialog.Filter = "JPEGファイル(*.jpg)|*.jpg";
            if (System.IO.Path.IsPathRooted(Path.SDstImg))
            {
                dialog.InitialDirectory = System.IO.Path.GetDirectoryName(Path.SDstImg);
            }
            if (dialog.ShowDialog() == true)
            {
                Path.SDstImg = dialog.FileName;
                UpdateWindow(SG.set);
            }
        }


        /////////////////////////////////////////////////////////////////////////
        // パス直接変更のコントロール
        /////////////////////////////////////////////////////////////////////////

        private void TextBox_TgtImgDir_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateWindow(SG.get);
        }

        private void TextBox_SecImgDir_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateWindow(SG.get);

        }

        private void TextBox_DstImgDir_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateWindow(SG.get);

        }


        /////////////////////////////////////////////////////////////////////////
        // チェック状態の変更
        private void CheckBox_SubDir_Click(object sender, RoutedEventArgs e)
        {
            UpdateWindow(SG.get);
        }

        /////////////////////////////////////////////////////////////////////////
        // D&D のコントロール
        /////////////////////////////////////////////////////////////////////////

        private void TextBox_TgtImgDir_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void TextBox_TgtImgDir_Drop(object sender, DragEventArgs e)
        {
            var dropFiles = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            if (dropFiles == null) return;
            TextBox_TgtImgDir.Text = dropFiles[0];
            UpdateWindow(SG.get);
        }

        private void TextBox_SecImgDir_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void TextBox_SecImgDir_Drop(object sender, DragEventArgs e)
        {
            var dropFiles = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            if (dropFiles == null) return;
            TextBox_SecImgDir.Text = dropFiles[0];
            UpdateWindow(SG.get);
        }

        private void TextBox_DstImgDir_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void TextBox_DstImgDir_Drop(object sender, DragEventArgs e)
        {
            var dropFiles = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            if (dropFiles == null) return;
            TextBox_DstImgDir.Text = dropFiles[0];
            UpdateWindow(SG.get);
        }

   }

}