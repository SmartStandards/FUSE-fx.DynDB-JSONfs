using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.ModelDescription;
using System.IO;
using System.IO.Abstraction;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Data.Fuse {



  ////////////////////////////////////////////////////////////////////////////////////////////////
  //
  //TODO: Sobald WinForms-Demo in FUSE-fx.WinForms fertig ist - diese auch hierher kopieren!
  //
  ////////////////////////////////////////////////////////////////////////////////////////////////



  [TestClass]
  public class DynDbTests {

    [TestMethod]
    public void Demotest() {

      string dataDir = Path.Combine(Assembly.GetExecutingAssembly().Location, "Data");
      Directory.CreateDirectory(dataDir);

      var dataAfs = new AfsLocalRepository(dataDir);
      var schema = new SchemaRoot();


      var engine = new JsonDbEngine(dataAfs, schema);

      //engine....

    }

  }

}
