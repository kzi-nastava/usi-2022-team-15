﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Klinika.GUI.Manager
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            roomsTable.DataSource = Repositories.RoomRepository.GetAll();
            roomsTable.ClearSelection();
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {

        }
    }
}
