using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.MVVM.ViewModel
{
    public class IngameControlViewModel : TabBase
    {
        public string TestString {  get; set; }

        private bool _isBlinking = false;

        public bool IsBlinking
        {
            get { return _isBlinking; }
            set { _isBlinking = value; }
        }

    }
}
