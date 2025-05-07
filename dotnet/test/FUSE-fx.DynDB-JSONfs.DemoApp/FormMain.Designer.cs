
namespace System.Data.Fuse.WinFormsDemo {

  partial class FormMain {

    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
      if (disposing && (components != null)) {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
      ComponentModel.ComponentResourceManager resources = new ComponentModel.ComponentResourceManager(typeof(FormMain));
      _FuseTable = new FuseTable();
      label1 = new System.Windows.Forms.Label();
      this.SuspendLayout();
      // 
      // _FuseTable
      // 
      _FuseTable.Anchor = Windows.Forms.AnchorStyles.Top | Windows.Forms.AnchorStyles.Bottom | Windows.Forms.AnchorStyles.Left | Windows.Forms.AnchorStyles.Right;
      _FuseTable.AutoSize = true;
      _FuseTable.Location = new System.Drawing.Point(12, 30);
      _FuseTable.Name = "_FuseTable";
      _FuseTable.Size = new System.Drawing.Size(657, 449);
      _FuseTable.TabIndex = 0;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Font = new System.Drawing.Font("Segoe UI", 9F, Drawing.FontStyle.Bold | Drawing.FontStyle.Italic, Drawing.GraphicsUnit.Point);
      label1.ForeColor = Drawing.Color.FromArgb(192, 0, 0);
      label1.Location = new System.Drawing.Point(12, 12);
      label1.Name = "label1";
      label1.Size = new System.Drawing.Size(449, 15);
      label1.TabIndex = 1;
      label1.Text = "TODO: Demo sollte FUSE-fx.DynDB-JSON fs (in AppData.Local) veranschlichen!";
      // 
      // FormMain
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
      this.AutoScaleMode = Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(681, 491);
      this.Controls.Add(label1);
      this.Controls.Add(_FuseTable);
      this.Icon = (Drawing.Icon)resources.GetObject("$this.Icon");
      this.Name = "FormMain";
      this.StartPosition = Windows.Forms.FormStartPosition.CenterScreen;
      this.Text = "FUSE-fx WinForms Demo (by Smart Standards)";
      this.ResumeLayout(false);
      this.PerformLayout();
    }

    #endregion

    private System.Data.Fuse.FuseTable _FuseTable;
    private Windows.Forms.Label label1;
  }
}

