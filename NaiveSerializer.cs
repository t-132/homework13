using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace homework13
{
   public class NaivStringeSerializer
   {
      public static char[] quote = { '\r', '\n'};
      public static char[] quoteTo = { 'r', 'n' };
      public static string Serialize(object obj)
      {
         return Serialize(obj, ',', '\\');
      }

      public static Object Deserialize(string str, Object obj)
      {
         return Deserialize(str, obj, ',', '\\');
      }
      
      public static string Serialize(Object obj, char delimeter, char? escapeQuote)
      {
         if (obj is null)
         {
            throw new ArgumentNullException("NaivStringeSerializer: null argument");
         }

         
         var result = new StringBuilder();         

         if(escapeQuote is null)
            return ToString(obj, ref result, delimeter, null).ToString();

         var quoteForm = new string[quote.Length + 2];
         var quoteTo2 = new string[quote.Length + 2];

         quoteForm[0] = escapeQuote.ToString();
         quoteForm[1] = delimeter.ToString();
         quoteTo2[0] = $"{escapeQuote}{escapeQuote}";
         quoteTo2[1] = $"{escapeQuote}{delimeter}";

         for (int i = 2; i < quoteTo2.Length ; i++) 
         { 
            quoteTo2[i] = $"{escapeQuote},{quoteTo[i-2]}";
            quoteForm[i] = quote[i-2].ToString();
         }


         ToString(obj, ref result, delimeter, x => { var s = new StringBuilder(x); for (var i = 0; i < quoteTo2.Length; i++) s.Replace(quoteForm[i], quoteTo2[i]); return s; });
         result.Remove(result.Length - 1, 1);
         return result.ToString();
      }
      
      public static Object Deserialize(string str, Object obj, char delimeter, char? escapeQuote)
      {
         if (obj is null)
         {
            throw new ArgumentNullException("NaivStringeSerializer: null argument");
         }

         if (escapeQuote is null )return SetObject(obj, GetToken(str, (x, y) => { if (y == delimeter) return (char)0xFFFF; return y; }));
         
         return SetObject(obj, GetToken(str, (x, y) => {                                                          
                                                         if (x == escapeQuote) 
                                                         { 
                                                            if (y == escapeQuote || y == delimeter) return y; 
                                                            for (var i = 0; i < quoteTo.Length; i++) if (x == quoteTo[i]) return quote[i];
                                                            return (char)0;
                                                         }
                                                         if (y == escapeQuote) return (char)0; 
                                                         if (y == delimeter) return (char)0xFFFF; 
                                                         return y; }
         ));
      }
      
      private static IEnumerator<string> GetToken(string str, Func<char,char,char> filter)
      {
         if (str.Length == 0) yield break;

         var s = new StringBuilder(64);
         char p = '\0',p1;
         char c;
         for (var i = 0; i < str.Length; i++)
         {
            c = filter(p, str[i]);
            p = '\0';
            if (c == 0xFFFF)
            {
               if (s.Length == 0) { yield return null; continue; }
               else
               {
                  var r = s.ToString();
                  s.Clear();
                  yield return r;
                  continue;
               }
            }
            if (c == 0) { p = str[i]; continue; }
            s.Append(c);
            
         }
         if (s.Length == 0) yield break;
         else { var r = s.ToString(); s.Clear(); yield return r; }
      }


      private static StringBuilder ToString(Object obj, ref StringBuilder result, char delimeter, Func<string, StringBuilder> filter)
      {
         if (obj is null)
         {
            throw new ArgumentNullException("NaivStringeSerializer: null argument");
         }

         var objType = obj.GetType();
         var props = objType.GetProperties();
         var fields = objType.GetFields();
         result.EnsureCapacity((props.Length + fields.Length) * 16);

         for (int i = 0; i < props.Length; i++)
         {
            if (props[i].SetMethod == null || props[i].GetMethod == null) continue; //не будем читать, то что не сможем потом установить и наоборот (может так плохо делать?)
            var tp = props[i].PropertyType.GetTypeInfo();
            //посчитаем string простым типом, но это не так!
            if (tp.IsValueType || tp.FullName == "System.String")
            {               
               if (!(filter is null))
               {
                  result.Append(filter(props[i].GetValue(obj).ToString()));                  
               }
               else
               {
                  result.Append(props[i].GetValue(obj).ToString());                  
               }
               result.Append(delimeter);
               continue;
            }
            if (tp.IsArray) throw new ArgumentException("Array arg not implement"); //устал уже1 сил нет.
            if (tp.IsClass)
            {
               ToString(props[i].GetValue(obj), ref result, delimeter, filter);
            }
            else throw new ArgumentException("Can not serialize non-class or  non-value type");
         }

         for (int i = 0; i < fields.Length; i++)
         {
            var tp = fields[i].FieldType.GetTypeInfo();

            if (tp.IsValueType || tp.FullName == "System.String")
            {
               if (!(filter is null))
               {
                  result.Append(filter(fields[i].GetValue(obj).ToString()));                  
               }
               else
               {
                  result.Append(fields[i].GetValue(obj).ToString());                  
               }               
               result.Append(delimeter);
               continue;
            }
            if (tp.IsArray) throw new ArgumentException("Array arg not implement"); //устал уже1 сил нет.
            if (tp.IsClass)
            {
               ToString(fields[i].GetValue(obj), ref result, delimeter, filter);
            }
            else throw new ArgumentException("Can not serialize non-class or  non-value type");
         }                            
         return result;
      }
      
      private static Object SetObject(Object obj, IEnumerator<string> GetToken)
      {
         if (obj is null)
         {
            throw new ArgumentNullException("NaivStringeSerializer: null argument");
         }

         var objType = obj.GetType();
         var props = objType.GetProperties();
         var fields = objType.GetFields();
         
         for (int i = 0; i < props.Length && GetToken.MoveNext(); i++)
         {
            if (props[i].SetMethod == null || props[i].GetMethod == null) continue; //не будем читать, то что не сможем потом установить и наоборот (может так плохо делать?)
            var tp = props[i].PropertyType.GetTypeInfo();
            //посчитаем string простым типом, но это не так!
            if (tp.FullName == "System.String")
            {
               props[i].SetValue(obj, GetToken.Current);
               continue;
            }
            if (tp.IsValueType)
            {
               var value = Activator.CreateInstance(tp);
               props[i].SetValue(obj, Convert.ChangeType(GetToken.Current, props[i].GetType()));
               continue;
            }
            if (tp.IsArray) throw new ArgumentException("Array arg not implement"); 
            if (tp.IsClass)
            {
               SetObject(props[i].GetValue(obj), GetToken);
            }
            else throw new ArgumentException("Can not serialize non-class or  non-value type");
         }

         for (int i = 0; i < fields.Length; i++)
         {
            var tp = fields[i].FieldType.GetTypeInfo();            

            if (tp.FullName == "System.String")
            {
               if (GetToken.MoveNext()) fields[i].SetValue(obj, GetToken.Current);               
               continue;
            }
            if (tp.IsValueType)
            {
               if (GetToken.MoveNext()) fields[i].SetValue(obj, Convert.ChangeType(GetToken.Current, tp));
               continue;
            }
            if (tp.IsArray) throw new ArgumentException("Array arg not implement"); //устал уже1 сил нет.            
            if (tp.IsClass)
            {
               if (fields[i].GetValue(obj) is null) fields[i].SetValue(obj, Activator.CreateInstance(tp));
               fields[i].SetValue(obj, SetObject(fields[i].GetValue(obj), GetToken));
            }
            else throw new ArgumentException("Can not serialize non-class or  non-value type");
         }
         return obj;
      }
   }
}
