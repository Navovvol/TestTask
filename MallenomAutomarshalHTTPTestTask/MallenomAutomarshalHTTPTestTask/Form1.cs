using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mallenom.Automarshal.HTTP.TestTask
{
    public partial class Form1 : Form
    {
        HttpRequestTable httpreq;
        public Form1()
        {
            InitializeComponent();
            httpreq = new HttpRequestTable();
     
            dataGridView1.DataSource = httpreq.Table;
            httpreq.RequestGet(0);
        }
    }
}
