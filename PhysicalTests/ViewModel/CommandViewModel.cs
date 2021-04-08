using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhysicalTests.Pages;

namespace PhysicalTests.ViewModel
{
    public delegate void GoToPsyTstPage();  //委托——跳转到PhysicalTestItemsPage

    public class CommandViewModel : ViewModelBase
    {
        public event GoToPsyTstPage GoToPsyTstPage; //事件——跳转到PhysicalTestItemsPage

        public CommandViewModel()
        {

        }

        #region 全局命令
        //Login页面的按钮事件
        private RelayCommand submitCmd;
        public RelayCommand SubmitCmd
        {
            get
            {
                if (submitCmd == null) return new RelayCommand(() => ExcuteUserIfo(), CanExcute);
                return submitCmd;
            }
            set { submitCmd = value; }
        }
        #endregion

        #region 附属方法
        //提交用户信息
        //跳转到下一个页面
        private void ExcuteUserIfo()
        {
            GoToPsyTstPage();
        }

        //按钮是否可点击
        private bool CanExcute()
        {
            return true;
        }
        #endregion
    }
}
