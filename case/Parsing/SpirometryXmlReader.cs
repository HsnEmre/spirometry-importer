using Spirometry.Importer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;

namespace Spirometry.Importer.Parsing
{
    public sealed class SpirometryXmlReader//xml dosyalarini okuyacak sinif
    {
        //belirtilen kuralla acip belge olarak dondur
        private static XDocument LoadDocument(string filePath)
        {
            //xml ayar
            var settings = new XmlReaderSettings
            {
                //dtd tanimi iceren xml dosyalarini rededer
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 1_000_000
            };

            using var stream = File.OpenRead(filePath);
            using var reader = XmlReader.Create(stream, settings);
            return XDocument.Load(reader);
        }


        //verilen elemanlarda bir tanesi olmasi zorunlu
        private static XElement RequireSingle(
            IEnumerable<XElement> elements,
            string fieldName)
        {
            var matches = elements.Take(2).ToArray();//birden fazla kayit olmadigni anlamak icin en fazla 2 element

            if (matches.Length != 1)
            {
                throw new InvalidDataException($"{fieldName} Tam olarak bir eleman bekleniyor");
            }
            return matches[0];
        }


        //zorunlu metin alani kontrolu
        private static string RequireText(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException($"{fieldName}");

            }

            return value.Trim();
        }



        public SpirometryTest Read(string filePath)
        {

            XDocument document = LoadDocument(filePath);


            XElement? root = document.Root;


            if (root is null || root.Name != "ndd")
            {

                throw new InvalidDataException("XML kok elemani ndd olmali.");
            }


            XElement command = RequireSingle(root.Elements("Command"), "Command");


            if ((string?)command.Attribute("Type") != "TestResult")
            {
                throw new InvalidDataException("Command Type, TestResult olmali.");
            }


            XElement orderElement = RequireSingle(command.Elements("Parameter").Where(element => (string?)element.Attribute("Name") == "OrderID"), "OrderID");


            string orderId = RequireText(orderElement.Value, "OrderID");


            XElement patients = RequireSingle(root.Elements("Patients"), "Patients");


            XElement patient = RequireSingle(patients.Elements("Patient"), "Patient");


            string patientId = RequireText(
                (string?)patient.Attribute("ID"),
                "Patient.ID");

            XElement intervals = RequireSingle(
                patient.Elements("Intervals"), "Intervals");


            XElement interval = RequireSingle(
                intervals.Elements("Interval"), "Interval");


            XElement tests = RequireSingle(interval.Elements("Tests"), "Tests");


            XElement testElement = RequireSingle(tests.Elements("Test"), "Test");


            string testType = RequireText(
                (string?)testElement.Attribute("TypeOfTest"),
                "TypeOfTest");

            if (testType != "FVC")
            {

                throw new InvalidDataException("Desteklenmeyen test turu.");
            }


            XElement dateElement = RequireSingle(
                testElement.Elements("TestDate"), "TestDate");


            string dateText = RequireText(dateElement.Value, "TestDate");


            bool validDate = DateTime.TryParseExact(
                dateText,
                "yyyy-MM-dd'T'HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime testDate);


            if (!validDate)
            {

                throw new InvalidDataException("TestDate gecersiz.");
            }


            var qualityElements = testElement.Elements("QualityGrade").Take(2).ToArray();


            if (qualityElements.Length > 1)
            {

                throw new InvalidDataException("QualityGrade birden fazla olamaz.");
            }


            string? qualityGrade = qualityElements.FirstOrDefault()?.Value.Trim();


            if (string.IsNullOrWhiteSpace(qualityGrade))
            {

                qualityGrade = null;
            }


            var result = new SpirometryTest
            {

                OrderId = orderId,


                DevicePatientId = patientId,


                TestType = testType,


                TestDate = DateTime.SpecifyKind(testDate, DateTimeKind.Unspecified),


                QualityGrade = qualityGrade
            };

            #region Helper
            XElement bestValues = RequireSingle(testElement.Elements("BestValues"), ("BestValues"));//best values bolumu

            //test icindeki tekrarlanan olcum kodlari takibi
            var measurementCodes = new HashSet<string>(StringComparer.Ordinal);

            foreach (XElement parameter in bestValues.Elements("ResultParameter"))
            {
                string code = RequireText((string?)parameter.Attribute("ID"), "ResultParameter.ID");

                if (!measurementCodes.Add(code))
                {
                    throw new InvalidDataException("tekrarlanan olcum kodu");
                }

                XElement unitElement = RequireSingle(
        parameter.Elements("Unit"), "Unit");

                string unit = RequireText(unitElement.Value, "Unit");

                //olculen deger zorunlu sayi
                decimal value = ReadDecimal(parameter, "DataValue", true) ?? throw new InvalidDataException("Data value Eksik");

                decimal? predictedValue = ReadDecimal(parameter, "PredictedValue", false);

                decimal? lowerLimit = ReadDecimal(parameter, "LLNormal", false);

                var measurement = new SpirometryMeasurement
                {
                    Code = code,
                    Value = value,
                    Unit = unit,
                    PredictedValue = predictedValue,
                    LowerLimitOfNormal = lowerLimit,
                };
                result.Measurements.Add(measurement);
            }

            if (result.Measurements.Count == 0)
            {
                throw new InvalidDataException("Testte olcum bulunamadi");
            }
            string[] expectedCodes = ["FVC", "FEV1", "FEV1_FVC", "PEF"];

            foreach (string expectedCode  in expectedCodes)
            {
                if (!measurementCodes.Contains(expectedCode))
                {
                    
                    result.Warnings.Add($"MISSING_MEASUREMENT:{expectedCode}");
                }
            }

            #endregion




            return result;
        }


        private static decimal? ReadDecimal(XElement parent, string elementName, bool required)//bir olcumun sayisal alanini okur
        {
            var elements = parent.Elements(elementName).Take(2).ToArray();
            if (elements.Length > 1)
            {
                throw new InvalidDataException($"{elementName}; birden fazla eleman bulundu");
            }

            string? text = elements.FirstOrDefault()?.Value.Trim(); //mevcut alan yoksa null don

            if (string.IsNullOrWhiteSpace(text))//alan yoksa veya degeri bossa bu bloga girer
            {
                if (required)
                {
                    throw new InvalidDataException($"{elementName} zorunlu sayisal alan bos");
                }
                return null;// istege bagli sifira cevirmeden null dondurur
            }

            var numberStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;//isaret ve ondalik noktasina izin ver

            bool validNumber = decimal.TryParse(text, numberStyles, CultureInfo.InvariantCulture, out decimal value);//

            if (!validNumber)
            {
                throw new InvalidDataException($"{elementName}: gecersiz sayisal deger");
            }
            return value;
        }

    }
}
