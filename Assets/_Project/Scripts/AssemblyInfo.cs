using System.Runtime.CompilerServices;

// Editor-Skripte und Tests dürfen die internen Befüll-Methoden der Datenobjekte aufrufen
// (zum Beispiel ItemKind.EditorSetBasics). Diese Methoden gibt es nur im Editor, nicht in der exe.
[assembly: InternalsVisibleTo("Schutzhuette.Editor")]
[assembly: InternalsVisibleTo("Schutzhuette.Tests.EditMode")]
[assembly: InternalsVisibleTo("Schutzhuette.Tests.PlayMode")]
