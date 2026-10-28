using System.ComponentModel;

namespace AshClicker
{

    partial class AshForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonLoadDriver = new System.Windows.Forms.Button();
            this.buttonSwitchEnable = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnName = new System.Windows.Forms.ColumnHeader();
            this.columnBinding = new System.Windows.Forms.ColumnHeader();
            this.buttonDeleteKey = new System.Windows.Forms.Button();
            this.buttonAddKey = new System.Windows.Forms.Button();
            this.preciseDelay = new System.Windows.Forms.CheckBox();
            this.buttonDriverless = new System.Windows.Forms.Button();
            this.buttonInstallInterception = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.soundEffect = new System.Windows.Forms.CheckBox();
            this.titleBar = new System.Windows.Forms.Panel();
            this.titleIcon = new System.Windows.Forms.PictureBox();
            this.titleLabel = new System.Windows.Forms.Label();
            this.buttonMinimize = new System.Windows.Forms.Button();
            this.buttonClose = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.buttonToggleHotkey = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.buttonPauseHotkey = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonLoadDriver
            // 
            this.buttonLoadDriver.Location = new System.Drawing.Point(235, 278);
            this.buttonLoadDriver.Name = "buttonLoadDriver";
            this.buttonLoadDriver.Size = new System.Drawing.Size(154, 34);
            this.buttonLoadDriver.TabIndex = 0;
            this.buttonLoadDriver.Text = "加载 DD 驱动";
            this.buttonLoadDriver.UseVisualStyleBackColor = true;
            this.buttonLoadDriver.Visible = false;
            this.buttonLoadDriver.Click += new System.EventHandler(this.button1_Click);
            // 
            // buttonSwitchEnable
            // 
            this.buttonSwitchEnable.Location = new System.Drawing.Point(233, 136);
            this.buttonSwitchEnable.Name = "buttonSwitchEnable";
            this.buttonSwitchEnable.Size = new System.Drawing.Size(154, 32);
            this.buttonSwitchEnable.TabIndex = 1;
            this.buttonSwitchEnable.Text = "按键开启";
            this.buttonSwitchEnable.UseVisualStyleBackColor = true;
            this.buttonSwitchEnable.Click += new System.EventHandler(this.button2_Click);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(17, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(372, 19);
            this.label2.TabIndex = 3;
            this.label2.Text = "当前状态：";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(96, 45);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(75, 19);
            this.label3.TabIndex = 4;
            this.label3.Text = "关闭";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(17, 86);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(154, 25);
            this.textBox1.TabIndex = 5;
            this.textBox1.Text = "20";
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(17, 141);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(154, 25);
            this.textBox2.TabIndex = 6;
            this.textBox2.Text = "5";
            this.textBox2.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(17, 64);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(178, 22);
            this.label4.TabIndex = 7;
            this.label4.Text = "连发间隔（毫秒）";
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(17, 116);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(178, 22);
            this.label5.TabIndex = 8;
            this.label5.Text = "抬起间隔（毫秒）";
            // 
            // listView1
            // 
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.columnName, this.columnBinding });
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = true;
            this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(17, 200);
            this.listView1.MultiSelect = false;
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(372, 158);
            this.listView1.TabIndex = 9;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // columnName
            // 
            this.columnName.Name = "columnName";
            this.columnName.Text = "方案名";
            this.columnName.Width = 140;
            // 
            // columnBinding
            // 
            this.columnBinding.Name = "columnBinding";
            this.columnBinding.Text = "绑定关系";
            this.columnBinding.Width = 226;
            // 
            // buttonDeleteKey
            // 
            this.buttonDeleteKey.Location = new System.Drawing.Point(235, 364);
            this.buttonDeleteKey.Name = "buttonDeleteKey";
            this.buttonDeleteKey.Size = new System.Drawing.Size(154, 34);
            this.buttonDeleteKey.TabIndex = 10;
            this.buttonDeleteKey.Text = "删除键位";
            this.buttonDeleteKey.UseVisualStyleBackColor = true;
            this.buttonDeleteKey.Click += new System.EventHandler(this.buttonDeleteKey_Click);
            // 
            // buttonAddKey
            // 
            this.buttonAddKey.Location = new System.Drawing.Point(17, 364);
            this.buttonAddKey.Name = "buttonAddKey";
            this.buttonAddKey.Size = new System.Drawing.Size(154, 34);
            this.buttonAddKey.TabIndex = 11;
            this.buttonAddKey.Text = "添加键位";
            this.buttonAddKey.UseVisualStyleBackColor = true;
            this.buttonAddKey.Click += new System.EventHandler(this.buttonAddKey_Click);
            // 
            // preciseDelay
            // 
            this.preciseDelay.Checked = true;
            this.preciseDelay.CheckState = System.Windows.Forms.CheckState.Checked;
            this.preciseDelay.Location = new System.Drawing.Point(283, 45);
            this.preciseDelay.Name = "preciseDelay";
            this.preciseDelay.Size = new System.Drawing.Size(104, 24);
            this.preciseDelay.TabIndex = 12;
            this.preciseDelay.Text = "精确延迟";
            this.preciseDelay.UseVisualStyleBackColor = true;
            // 
            // buttonDriverless
            // 
            this.buttonDriverless.Location = new System.Drawing.Point(233, 281);
            this.buttonDriverless.Name = "buttonDriverless";
            this.buttonDriverless.Size = new System.Drawing.Size(154, 34);
            this.buttonDriverless.TabIndex = 13;
            this.buttonDriverless.Text = "无驱动模式";
            this.buttonDriverless.UseVisualStyleBackColor = true;
            this.buttonDriverless.Visible = false;
            this.buttonDriverless.Click += new System.EventHandler(this.buttonDriverless_Click);
            // 
            // buttonInstallInterception
            // 
            this.buttonInstallInterception.Location = new System.Drawing.Point(233, 67);
            this.buttonInstallInterception.Name = "buttonInstallInterception";
            this.buttonInstallInterception.Size = new System.Drawing.Size(154, 34);
            this.buttonInstallInterception.TabIndex = 14;
            this.buttonInstallInterception.Text = "安装驱动";
            this.buttonInstallInterception.UseVisualStyleBackColor = true;
            this.buttonInstallInterception.Click += new System.EventHandler(this.buttonInstallInterception_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(233, 101);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(154, 34);
            this.button1.TabIndex = 15;
            this.button1.Text = "卸载驱动";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // soundEffect
            // 
            this.soundEffect.Checked = true;
            this.soundEffect.CheckState = System.Windows.Forms.CheckState.Checked;
            this.soundEffect.Location = new System.Drawing.Point(173, 45);
            this.soundEffect.Name = "soundEffect";
            this.soundEffect.Size = new System.Drawing.Size(104, 24);
            this.soundEffect.TabIndex = 17;
            this.soundEffect.Text = "音效提示";
            this.soundEffect.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(17, 416);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(108, 22);
            this.label6.TabIndex = 18;
            this.label6.Text = "开关快捷键";
            // 
            // buttonToggleHotkey
            // 
            this.buttonToggleHotkey.Location = new System.Drawing.Point(130, 412);
            this.buttonToggleHotkey.Name = "buttonToggleHotkey";
            this.buttonToggleHotkey.Size = new System.Drawing.Size(259, 30);
            this.buttonToggleHotkey.TabIndex = 19;
            this.buttonToggleHotkey.Text = "未设置（点击设置）";
            this.buttonToggleHotkey.UseVisualStyleBackColor = true;
            this.buttonToggleHotkey.Click += new System.EventHandler(this.buttonToggleHotkey_Click);
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(17, 450);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(108, 22);
            this.label7.TabIndex = 20;
            this.label7.Text = "暂停键";
            // 
            // buttonPauseHotkey
            // 
            this.buttonPauseHotkey.Location = new System.Drawing.Point(130, 446);
            this.buttonPauseHotkey.Name = "buttonPauseHotkey";
            this.buttonPauseHotkey.Size = new System.Drawing.Size(259, 30);
            this.buttonPauseHotkey.TabIndex = 21;
            this.buttonPauseHotkey.Text = "未设置（点击设置）";
            this.buttonPauseHotkey.UseVisualStyleBackColor = true;
            this.buttonPauseHotkey.Click += new System.EventHandler(this.buttonPauseHotkey_Click);
            // 
            // titleBar
            // 
            this.titleBar.BackColor = System.Drawing.Color.FromArgb(250, 250, 250);
            this.titleBar.Location = new System.Drawing.Point(0, 0);
            this.titleBar.Name = "titleBar";
            this.titleBar.Size = new System.Drawing.Size(405, 30);
            this.titleBar.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TitleBar_MouseDown);
            // 
            // titleIcon
            // 
            this.titleIcon.BackColor = System.Drawing.Color.Transparent;
            this.titleIcon.Location = new System.Drawing.Point(10, 7);
            this.titleIcon.Name = "titleIcon";
            this.titleIcon.Size = new System.Drawing.Size(16, 16);
            this.titleIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.titleIcon.TabStop = false;
            this.titleIcon.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TitleBar_MouseDown);
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.BackColor = System.Drawing.Color.Transparent;
            this.titleLabel.Location = new System.Drawing.Point(32, 6);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(60, 20);
            this.titleLabel.Text = "无情按键";
            this.titleLabel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TitleBar_MouseDown);
            // 
            // buttonMinimize
            // 
            this.buttonMinimize.FlatAppearance.BorderSize = 0;
            this.buttonMinimize.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.buttonMinimize.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonMinimize.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.buttonMinimize.Location = new System.Drawing.Point(317, 0);
            this.buttonMinimize.Name = "buttonMinimize";
            this.buttonMinimize.Size = new System.Drawing.Size(44, 30);
            this.buttonMinimize.TabIndex = 30;
            this.buttonMinimize.TabStop = false;
            this.buttonMinimize.Text = "—";
            this.buttonMinimize.UseVisualStyleBackColor = false;
            this.buttonMinimize.Click += new System.EventHandler(this.buttonMinimize_Click);
            // 
            // buttonClose
            // 
            this.buttonClose.FlatAppearance.BorderSize = 0;
            this.buttonClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(232, 17, 35);
            this.buttonClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonClose.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.buttonClose.Location = new System.Drawing.Point(361, 0);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(44, 30);
            this.buttonClose.TabIndex = 31;
            this.buttonClose.TabStop = false;
            this.buttonClose.Text = "✕";
            this.buttonClose.UseVisualStyleBackColor = false;
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // AshForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(405, 516);
            this.titleBar.Controls.Add(this.titleIcon);
            this.titleBar.Controls.Add(this.titleLabel);
            this.titleBar.Controls.Add(this.buttonMinimize);
            this.titleBar.Controls.Add(this.buttonClose);
            this.Controls.Add(this.titleBar);
            this.Controls.Add(this.buttonPauseHotkey);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.buttonToggleHotkey);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.soundEffect);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.buttonInstallInterception);
            this.Controls.Add(this.preciseDelay);
            this.Controls.Add(this.buttonAddKey);
            this.Controls.Add(this.buttonDeleteKey);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.buttonSwitchEnable);
            this.Controls.Add(this.buttonDriverless);
            this.Controls.Add(this.buttonLoadDriver);
            this.Icon = global::AshClicker.Properties.Resources.ashlogo;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MaximizeBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "AshForm";
            this.Text = "无情按键";
            this.Load += new System.EventHandler(this.AshForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.CheckBox soundEffect;

        private System.Windows.Forms.Panel titleBar;
        private System.Windows.Forms.PictureBox titleIcon;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Button buttonMinimize;
        private System.Windows.Forms.Button buttonClose;

        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button buttonToggleHotkey;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button buttonPauseHotkey;

        private System.Windows.Forms.Button button1;

        private System.Windows.Forms.Button buttonInstallInterception;

        private System.Windows.Forms.Button buttonDriverless;

        private System.Windows.Forms.CheckBox preciseDelay;

        private System.Windows.Forms.Button buttonAddKey;

        private System.Windows.Forms.ColumnHeader columnName;
        private System.Windows.Forms.ColumnHeader columnBinding;

        private System.Windows.Forms.ListView listView1;

        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;

        private System.Windows.Forms.TextBox textBox2;

        private System.Windows.Forms.TextBox textBox1;

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;

        private System.Windows.Forms.Button buttonSwitchEnable;

        private System.Windows.Forms.Button buttonLoadDriver;

        #endregion

        private System.Windows.Forms.Button buttonDeleteKey;
    }
}