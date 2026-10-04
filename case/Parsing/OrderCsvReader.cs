using Microsoft.VisualBasic.FileIO;
using Spirometry.Importer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Spirometry.Importer.Parsing
{
    public sealed class OrderCsvReader
    {
        public List<Order> Read(string filepath)
        {
            //okunan  isleri tut liste
            var orders = new List<Order>();

            //is emrini tekrar kullanmayi engelle
            var orderIds = new HashSet<string>(StringComparer.Ordinal);


            //csv okuyucu
            using var parser = new TextFieldParser(filepath, Encoding.UTF8);
            parser.SetDelimiters(",");

            //virgulleri engelle
            parser.HasFieldsEnclosedInQuotes = true;


            parser.TrimWhiteSpace = true;

            string[] expectexheaders = [//bekelnen stun ve sira
                "order_id",
                "tenant_id",
                "tenant_name",
                "employee_ref",
                "order_date"
             ];

            string[]? headers = parser.ReadFields();//stun adlari oku

            if (headers is null ||
                !headers.SequenceEqual(expectexheaders,StringComparer.Ordinal)) //istenen sirada degilse reddedicez
            {
                throw new InvalidDataException("csv basliklari beklenen formatta degil"); 
            }

            //oku
            while (!parser.EndOfData)
            {
                long lineNumber = parser.LineNumber;//hata mesajda kullanmak icin satir

                string[]? fields=parser.ReadFields();//alanlara ayir

                // 5 alan sart

                if (fields is null || fields.Length != expectexheaders.Length)
                {
                    throw new InvalidDataException($"csv satir {lineNumber}: 5 alan bekleniyor");
                }

                if (fields.Any(string.IsNullOrWhiteSpace)) //zorunlu alan kontrolu
                {
                    throw new InvalidDataException($"csv satir {lineNumber} zorunlu alan bos");

                    
                }


                bool validDate = DateOnly.TryParseExact(
                        fields[4],
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,//dil ve kultur bagimsiz pcden
                        DateTimeStyles.None,
                        out DateOnly orderDate);


                if (!validDate)//gecerli degilse red
                {
                    throw new InvalidDataException($"csv satir {lineNumber} gecersiz");

                }

                if (!orderIds.Add(fields[0]))//kumeye ekle
                {
                    throw new InvalidDataException($"cvs satir {lineNumber} tekrarlana is emri kimligi");
                }

                //dogrulandi nesne olustur
                var order = new Order
                {
                    OrderId = fields[0],
                    TenantId = fields[1],
                    TenantName = fields[2],
                    EmployeeRef = fields[3],
                    OrderDate = orderDate,
                };
                orders.Add(order);//ekle

            }

            //yalnizca baslik iceren dosya kabul edilir,is emri bulunmayan dosya red
            if (orders.Count == 0)
            {
                throw new InvalidDataException($"csv dosyasi is emri yok");

            }
            return orders;//dosya tamamen dogrulandiktan sonra 
        }
    }
}
