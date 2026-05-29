
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
      ExpressionTree expressionTree1 = new ExpressionTree();
      ComponentModel.ComponentResourceManager resources = new ComponentModel.ComponentResourceManager(typeof(FormMain));
      _FuseTable = new FuseTable();
      _BtnAddNewEntity = new Windows.Forms.Button();
      this.SuspendLayout();
      // 
      // _FuseTable
      // 
      _FuseTable.Anchor = Windows.Forms.AnchorStyles.Top | Windows.Forms.AnchorStyles.Bottom | Windows.Forms.AnchorStyles.Left | Windows.Forms.AnchorStyles.Right;
      _FuseTable.AutoSize = true;
      _FuseTable.EntitySchema = null;
      expressionTree1.MatchAll = true;
      expressionTree1.Negate = false;
      expressionTree1.SubTree = null;
      _FuseTable.ExpressionTree = expressionTree1;
      _FuseTable.Limit = 100;
      _FuseTable.Location = new Drawing.Point(12, 30);
      _FuseTable.Name = "_FuseTable";
      _FuseTable.Size = new Drawing.Size(657, 449);
      _FuseTable.Skip = 0;
      _FuseTable.SortedBy = null;
      _FuseTable.TabIndex = 0;
      // 
      // _BtnAddNewEntity
      // 
      _BtnAddNewEntity.Location = new Drawing.Point(556, 12);
      _BtnAddNewEntity.Name = "_BtnAddNewEntity";
      _BtnAddNewEntity.Size = new Drawing.Size(113, 38);
      _BtnAddNewEntity.TabIndex = 2;
      _BtnAddNewEntity.Text = "Add new Entity";
      _BtnAddNewEntity.UseVisualStyleBackColor = true;
      _BtnAddNewEntity.Click += this._BtnAddNewEntity_Click;
      // 
      // FormMain
      // 
      this.AutoScaleDimensions = new Drawing.SizeF(7F, 15F);
      this.AutoScaleMode = Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new Drawing.Size(681, 491);
      this.Controls.Add(_BtnAddNewEntity);
      this.Controls.Add(_FuseTable);
      this.Icon = (Drawing.Icon)resources.GetObject("$this.Icon");
      this.Name = "FormMain";
      this.StartPosition = Windows.Forms.FormStartPosition.CenterScreen;
      this.Text = "FUSE-fx WinForms Demo (by Smart Standards)";
      this.Load += this.FormMain_Load;
      this.ResumeLayout(false);
      this.PerformLayout();
    }

    #endregion

    private System.Data.Fuse.FuseTable _FuseTable;
    private Windows.Forms.Button _BtnAddNewEntity;
  }
}

