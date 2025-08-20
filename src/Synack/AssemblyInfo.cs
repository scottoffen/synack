using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// In SDK-style projects such as this one, several assembly attributes that were historically
// defined in this file are now automatically added during build and populated with
// values defined in project properties. For details of which attributes are included
// and how to customize this process see: https://aka.ms/assembly-info-properties

// Setting ComVisible to false makes the types in this assembly not visible to COM
// components.  If you need to access a type in this assembly from COM, set the ComVisible
// attribute to true on that type. See https://aka.ms/assembly-visibility
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM.
//[assembly: Guid("")]

// CLSCompliant attribute is used to indicate whether the assembly is compliant with
// the Common Language Specification (CLS). See https://aka.ms/assembly-cls
[assembly: CLSCompliant(true)]

// InternalsVisibleTo attribute is used to specify that the internal types of this assembly
// are visible to another assembly. This is often used for unit testing purposes.
// The specified assembly name must match the name of the assembly that will access the internal types.
// See https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.internalsvisibletoattribute
[assembly: InternalsVisibleTo("Synack.Tests")]
