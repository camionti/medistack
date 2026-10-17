using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace MediStack.Web
{
    public static class ExportadorUtil
    {
        public static DataTable PrepararTablaParaReporte(DataTable origen, Dictionary<string, string> mapeoColumnas)
        {
            DataTable limpia = new DataTable();
            if (origen == null) return limpia;

            foreach (KeyValuePair<string, string> col in mapeoColumnas)
            {
                if (origen.Columns.Contains(col.Key))
                {
                    limpia.Columns.Add(col.Value, typeof(string));
                }
            }

            foreach (DataRow filaOrigen in origen.Rows)
            {
                DataRow nuevaFila = limpia.NewRow();
                foreach (KeyValuePair<string, string> col in mapeoColumnas)
                {
                    if (origen.Columns.Contains(col.Key))
                    {
                        object valor = filaOrigen[col.Key];
                        if (valor == null || valor == DBNull.Value)
                        {
                            nuevaFila[col.Value] = "-";
                        }
                        else if (valor is DateTime fecha)
                        {
                            nuevaFila[col.Value] = fecha.TimeOfDay.TotalSeconds == 0
                                ? fecha.ToString("dd/MM/yyyy")
                                : fecha.ToString("dd/MM/yyyy HH:mm");
                        }
                        else if (valor is decimal monto)
                        {
                            nuevaFila[col.Value] = "$ " + monto.ToString("N2");
                        }
                        else if (valor is bool booleano)
                        {
                            nuevaFila[col.Value] = booleano ? "Sí" : "No";
                        }
                        else
                        {
                            string texto = Convert.ToString(valor).Trim();
                            nuevaFila[col.Value] = string.IsNullOrEmpty(texto) ? "-" : texto;
                        }
                    }
                }
                limpia.Rows.Add(nuevaFila);
            }

            return limpia;
        }

        public static void ExportarCSV(DataTable dt, string nombreArchivo)
        {
            HttpResponse response = HttpContext.Current.Response;
            response.Clear();
            response.Buffer = true;
            response.ContentType = "text/csv; charset=utf-8";
            response.AddHeader("content-disposition", "attachment;filename=" + nombreArchivo + ".csv");

            byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
            response.BinaryWrite(bom);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                sb.Append("\"" + dt.Columns[i].ColumnName.Replace("\"", "\"\"") + "\"");
                if (i < dt.Columns.Count - 1) sb.Append(";");
            }
            sb.AppendLine();

            foreach (DataRow row in dt.Rows)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    string valor = Convert.ToString(row[i])
                        .Replace("\"", "\"\"")
                        .Replace("\r", " ")
                        .Replace("\n", " ");
                    sb.Append("\"" + valor + "\"");
                    if (i < dt.Columns.Count - 1) sb.Append(";");
                }
                sb.AppendLine();
            }

            byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
            response.BinaryWrite(buffer);
            response.Flush();
            response.SuppressContent = true;
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        public static void ExportarPDF(DataTable dt, string nombreArchivo, string tituloDocumento, string subtitulo = "")
        {
            HttpResponse response = HttpContext.Current.Response;
            response.Clear();
            response.Buffer = true;
            response.ContentType = "application/pdf";
            response.AddHeader("content-disposition", "attachment;filename=" + nombreArchivo + ".pdf");

            using (MemoryStream ms = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4.Rotate(), 25f, 25f, 30f, 30f);
                PdfWriter.GetInstance(pdfDoc, ms);
                pdfDoc.Open();

                BaseColor colorPrimario = new BaseColor(2, 132, 199);    
                BaseColor colorTextoOscuro = new BaseColor(15, 23, 42);  
                BaseColor colorTextoGris = new BaseColor(100, 116, 139); 
                BaseColor colorFilaAlterna = new BaseColor(248, 250, 252);
                BaseColor colorBorde = new BaseColor(226, 232, 240);

                Font fontMarca = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, colorPrimario);
                Font fontTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, colorTextoOscuro);
                Font fontSubtitulo = FontFactory.GetFont(FontFactory.HELVETICA, 9, colorTextoGris);
                Font fontCabecera = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.WHITE);
                Font fontCelda = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, colorTextoOscuro);

                Paragraph pMarca = new Paragraph("MEDISTACK · GESTIÓN CLÍNICA", fontMarca);
                pdfDoc.Add(pMarca);

                Paragraph pTitulo = new Paragraph(tituloDocumento, fontTitulo);
                pTitulo.SpacingBefore = 2f;
                pdfDoc.Add(pTitulo);

                string infoEmision = "Fecha de emisión: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " hs · Total de registros: " + dt.Rows.Count;
                if (!string.IsNullOrWhiteSpace(subtitulo))
                {
                    infoEmision = subtitulo + "  |  " + infoEmision;
                }

                Paragraph pSub = new Paragraph(infoEmision, fontSubtitulo);
                pSub.SpacingBefore = 3f;
                pSub.SpacingAfter = 14f;
                pdfDoc.Add(pSub);

                int numColumnas = dt.Columns.Count > 0 ? dt.Columns.Count : 1;
                PdfPTable tabla = new PdfPTable(numColumnas);
                tabla.WidthPercentage = 100;
                tabla.HeaderRows = 1;

                foreach (DataColumn col in dt.Columns)
                {
                    PdfPCell celdaCab = new PdfPCell(new Phrase(col.ColumnName.ToUpperInvariant(), fontCabecera));
                    celdaCab.BackgroundColor = colorPrimario;
                    celdaCab.BorderColor = colorPrimario;
                    celdaCab.PaddingTop = 7f;
                    celdaCab.PaddingBottom = 7f;
                    celdaCab.PaddingLeft = 6f;
                    celdaCab.PaddingRight = 6f;
                    celdaCab.HorizontalAlignment = Element.ALIGN_LEFT;
                    tabla.AddCell(celdaCab);
                }

                if (dt.Rows.Count == 0)
                {
                    PdfPCell celdaVacia = new PdfPCell(new Phrase("No se encontraron registros para los filtros seleccionados.", fontSubtitulo));
                    celdaVacia.Colspan = numColumnas;
                    celdaVacia.Padding = 12f;
                    celdaVacia.HorizontalAlignment = Element.ALIGN_CENTER;
                    celdaVacia.BorderColor = colorBorde;
                    tabla.AddCell(celdaVacia);
                }
                else
                {
                    bool alternar = false;
                    foreach (DataRow fila in dt.Rows)
                    {
                        foreach (object item in fila.ItemArray)
                        {
                            PdfPCell celda = new PdfPCell(new Phrase(Convert.ToString(item), fontCelda));
                            celda.BackgroundColor = alternar ? colorFilaAlterna : BaseColor.WHITE;
                            celda.BorderColor = colorBorde;
                            celda.PaddingTop = 6f;
                            celda.PaddingBottom = 6f;
                            celda.PaddingLeft = 6f;
                            celda.PaddingRight = 6f;
                            tabla.AddCell(celda);
                        }
                        alternar = !alternar;
                    }
                }

                pdfDoc.Add(tabla);
                pdfDoc.Close();

                byte[] bytesPdf = ms.ToArray();
                response.BinaryWrite(bytesPdf);
                response.Flush();
                response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }
    }
}