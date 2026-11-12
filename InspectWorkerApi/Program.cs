using System;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;

void Dump(Type t)
{
    Console.WriteLine("TYPE: " + t.FullName);
    foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
    {
        Console.WriteLine($"  PROP {p.PropertyType.FullName} {p.Name} set={p.SetMethod != null} get={p.GetMethod != null}");
    }
    foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(m => !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_")))
    {
        Console.WriteLine($"  METHOD {m.ReturnType.FullName} {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name))})");
    }
    Console.WriteLine();
}

Dump(typeof(RetryContext));
Dump(typeof(BindingMetadata));
Dump(typeof(FunctionParameter));
