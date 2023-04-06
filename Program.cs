using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
namespace homework13
{
   class Program
   {
      public class F { public int i1, i2, i3, i4, i5; public F Get() => new F() { i1 = 1, i2 = 2, i3 = 3, i4 = 4, i5 = 5 }; }
      public class G { public string s = "\\\\\r\n,,,,,;;;"; public F f; public G Get() { G g = new G(); g.f = new F().Get(); return g; } }
      static void Main(string[] args)
      {
         Console.WriteLine("Hello World!");
         var f = new F();
         var g = new G();
         var s = NaivStringeSerializer.Serialize(g.Get());
         g = (G)NaivStringeSerializer.Deserialize(s, g);

         string[] str = new string[10000];

         var stopwatch = new Stopwatch();
         stopwatch.Start();
         for (int i = 0; i < 10000; i++)
            str[i] = NaivStringeSerializer.Serialize(f);
         stopwatch.Stop();
         Console.WriteLine($"Сериализация в строку: {stopwatch.ElapsedMilliseconds}");
         stopwatch.Reset();

         stopwatch.Start();
         for (int i = 0; i < 10000; i++)
             f = (F)NaivStringeSerializer.Deserialize(str[i],f);
         stopwatch.Stop();
         Console.WriteLine($"Десериализация из строки: {stopwatch.ElapsedMilliseconds}");


         stopwatch.Reset();
         stopwatch.Start();
         for (int i = 0; i < 10000; i++)
            str[i] = JsonSerializer.Serialize(f);
         stopwatch.Stop();
         Console.WriteLine($"Сериализация в JSON: {stopwatch.ElapsedMilliseconds}");

         stopwatch.Reset();
         stopwatch.Start();
         for (int i = 0; i < 10000; i++)
            JsonSerializer.Deserialize<F>(str[i]);
         stopwatch.Stop();
         Console.WriteLine($"Десериализация из JSON: {stopwatch.ElapsedMilliseconds}");
         stopwatch.Reset();

         Console.ReadLine();
      }
   }
}
