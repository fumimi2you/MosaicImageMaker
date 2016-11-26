using System;
using System.Windows;
using System.IO;

using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs; //CommonOpenFileDialog


namespace MosaicImageMaker
{
    public class ImgPath
    {
        public string[] asSrcImg;
        public string sSrcDir;
        public string sTgtImg;
        public string sDstImg;
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

        enum SG { set, get };

        public MainWindow()
        {
            InitializeComponent();

            m_iniUtl = new InifileUtils();
            m_path = new ImgPath();

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
                m_iniUtl.setValue("Path", "TgtImg", m_path.sTgtImg);
                m_iniUtl.setValue("Path", "SrcDir", m_path.sSrcDir);
                m_iniUtl.setValue("Path", "DstImg", m_path.sDstImg);
            }
            else
            {
                m_path.sTgtImg = m_iniUtl.getValueString("Path", "TgtImg");
                m_path.sSrcDir = m_iniUtl.getValueString("Path", "SrcDir");
                m_path.sDstImg = m_iniUtl.getValueString("Path", "DstImg");
            }
        }

        private void UpdateWindow(SG sg)
        {
            if (sg == SG.set)
            {
                TextBox_TgtImgDir.Text = m_path.sTgtImg;
                TextBox_SecImgDir.Text = m_path.sSrcDir;
                TextBox_DstImgDir.Text = m_path.sDstImg;
            }
            else
            {
                m_path.sTgtImg = TextBox_TgtImgDir.Text;
                m_path.sSrcDir = TextBox_SecImgDir.Text;
                m_path.sDstImg = TextBox_DstImgDir.Text;
            }

            UpdateWindow_SecImgDir();
        }

        private void UpdateWindow_SecImgDir()
        {
            try
            {
                m_path.asSrcImg = System.IO.Directory.GetFiles(
                    m_path.sSrcDir, "*.jpg", System.IO.SearchOption.TopDirectoryOnly);
                TextBox_SecImgDir.Text = m_path.sSrcDir;
                if(m_path.asSrcImg.Length>= DEF.LET_IMG_MIN)
                {
                    TextBlock_DrcImgCnt.Text = m_path.asSrcImg.Length.ToString("#,0") + "[枚] の画像が使えるっぽい。";
                }
                else
                {
                    TextBlock_DrcImgCnt.Text = m_path.asSrcImg.Length.ToString("#,0") + "[枚] 画像があるけど..."
                        + DEF.LET_IMG_MIN + "枚以上は画像欲しいかも？" ;
                }
            }
            catch (Exception e)
            {
                TextBlock_DrcImgCnt.Text = "";
            }
        }


        private void SetTgtPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "目標画像ファイルを開く";
            dialog.Filter = "JPEGファイル(*.jpg)|*.jpg";
            dialog.InitialDirectory = Path.GetDirectoryName(m_path.sTgtImg);
            if (dialog.ShowDialog() == true)
            {
                m_path.sTgtImg = dialog.FileName;
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
            Dialog.DefaultDirectory = m_path.sSrcDir;
            // 開く
            var Result = Dialog.ShowDialog();
            // もし開かれているなら
            if (Result == CommonFileDialogResult.Ok)
            {
                m_path.sSrcDir = Dialog.FileName;
                UpdateWindow(SG.set);
            }
        }

        private void SetDstPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "主力画像ファイルを保存";
            dialog.Filter = "JPEGファイル(*.jpg)|*.jpg";
            dialog.InitialDirectory = Path.GetDirectoryName(m_path.sDstImg);
            if (dialog.ShowDialog() == true)
            {
                m_path.sDstImg = dialog.FileName;
                UpdateWindow(SG.set);
            }
        }

        private async void Button_Execute_Click(object sender, RoutedEventArgs e)
        {
            UpdateWindow(SG.get);
            UpdateIni(SG.set);


            // Progressクラスのインスタンスを生成
            var spProg1 = new Progress<int>(ShowProgress1);
            var spProg2 = new Progress<int>(ShowProgress2);

            progressBar1.Maximum = m_path.asSrcImg.Length;
            progressBar1.Value = 0;

            progressBar2.Maximum = DEF.PERCENT_MAX;
            progressBar2.Value = 0;


            //  実処理実行
            await MosImgCore.Do(m_path, spProg1, spProg2);

            if (progressBar2.Value == DEF.PERCENT_MAX)
            {
                TextBlock_Report.Text = "できたよー (⌒∇⌒)";
            }
            else
            {
                TextBlock_Report.Text = "なんか失敗したっぽい。";
            }

            GC.Collect();
        }

        // 進捗を表示するメソッド（これはUIスレッドで呼び出される）
        private void ShowProgress1(int iVal)
        {
            progressBar1.Value = iVal;
            TextBlock_Report.Text = "まずは一通り素材画像を見てみるー " + iVal.ToString() + "/" + progressBar1.Maximum + " [枚]";
        }
        private void ShowProgress2(int iVal)
        {
            progressBar2.Value = iVal;
            TextBlock_Report.Text = "タイリングの組み合わせを考えてるー " + iVal.ToString() + "/" + progressBar2.Maximum + " [％]";
        }

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
    }

}