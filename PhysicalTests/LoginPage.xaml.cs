using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PhysicalTests.ViewModel;
using PhysicalTests.Pages;

namespace PhysicalTests.Pages
{
    /// <summary>
    /// LoginPage.xaml 的交互逻辑
    /// </summary>
    public partial class LoginPage : Page
    {

        public LoginPage()
        {
            InitializeComponent();
            CommandViewModel command = new CommandViewModel();
            this.DataContext = command; //手动设置前台的DataContext
            command.GoToPsyTstPage += Command_goToPsyTstPage;   //订阅跳转页面事件
        }

        //在CommandViewModel中会调用此方法 跳转到PhysicalTestItemsPage
        public void Command_goToPsyTstPage()
        {
            NavigationService.Navigate(new PhysicalTestItemsPage());
        }
    }
}
