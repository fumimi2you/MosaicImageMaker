using System;
using System.Runtime.InteropServices;
using System.Text;


namespace MosaicImageMaker
{

    /// <summary>
    /// ini�t�@�C����舵���̂��߂̃��[�e�B���e�B�N���X
    /// </summary>
    class InifileUtils
    {
        /// <summary>
        /// ini�t�@�C���̃p�X��ێ�
        /// </summary>
        private String filePath { get; set; }

        // ==========================================================
        [DllImport("KERNEL32.DLL")]
        public static extern uint
            GetPrivateProfileString(string lpAppName,
            string lpKeyName, string lpDefault,
            StringBuilder lpReturnedString, uint nSize,
            string lpFileName);

        [DllImport("KERNEL32.DLL")]
        public static extern uint
            GetPrivateProfileInt(string lpAppName,
            string lpKeyName, int nDefault, string lpFileName);

        [DllImport("kernel32.dll")]
        private static extern int WritePrivateProfileString(
            string lpApplicationName,
            string lpKeyName,
            string lpstring,
            string lpFileName);
        // ==========================================================

        /// <summary>
        /// �R���X�g���N�^(�f�t�H���g)
        /// </summary>
        public InifileUtils()
        {
            this.filePath = AppDomain.CurrentDomain.BaseDirectory + "default.ini";
        }

        /// <summary>
        /// �R���X�g���N�^(file�p�X���w�肷��ꍇ)
        /// </summary>
        /// <param name="filePath">ini�t�@�C���p�X</param>
        public InifileUtils(String filePath)
        {
            this.filePath = filePath;
        }

        /// <summary>
        /// ini�t�@�C�����̃Z�N�V�����̃L�[���w�肵�āA�������Ԃ�
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public String getValueString(String section, String key)
        {
            StringBuilder sb = new StringBuilder(1024);

            GetPrivateProfileString(
                section,
                key,
                "",
                sb,
                Convert.ToUInt32(sb.Capacity),
                filePath);

            return sb.ToString();
        }

        /// <summary>
        /// ini�t�@�C�����̃Z�N�V�����̃L�[���w�肵�āA�����l��Ԃ�
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public int getValueInt(String section, String key)
        {
            return (int)GetPrivateProfileInt(section, key, 0, filePath);
        }

        /// <summary>
        /// �w�肵���Z�N�V�����A�L�[�ɐ��l����������
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public void setValue(String section, String key, int val)
        {
            setValue(section, key, val.ToString());
        }

        /// <summary>
        /// �w�肵���Z�N�V�����A�L�[�ɕ��������������
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public void setValue(String section, String key, String val)
        {
            WritePrivateProfileString(section, key, val, filePath);
        }
    }
}