using MediStack.Dominio;
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
        public static byte[] GenerarBytesComprobantePDF(ComprobanteCobroModel c)
        {
            if (c == null) throw new ArgumentNullException("c");

            using (MemoryStream ms = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 40f, 40f, 40f, 40f);
                PdfWriter.GetInstance(pdfDoc, ms);
                pdfDoc.Open();

                BaseColor colorPrimario = new BaseColor(2, 132, 199);
                BaseColor colorTextoOscuro = new BaseColor(15, 23, 42);
                BaseColor colorTextoGris = new BaseColor(100, 116, 139);
                BaseColor colorFondoGris = new BaseColor(248, 250, 252);
                BaseColor colorBorde = new BaseColor(226, 232, 240);
                BaseColor colorTotalFondo = new BaseColor(240, 249, 255);

                Font fontMarca = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, colorPrimario);
                Font fontMarcaSub = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, colorTextoGris);
                Font fontTituloDoc = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, colorTextoOscuro);
                Font fontDocMeta = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, colorTextoGris);
                Font fontSeccion = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9.5f, colorPrimario);
                Font fontEtiqueta = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8.5f, colorTextoOscuro);
                Font fontValor = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, colorTextoOscuro);
                Font fontTablaCab = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9f, BaseColor.WHITE);
                Font fontTotal = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11f, colorPrimario);
                Font fontPie = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8f, colorTextoGris);

                // 1) Cabecera institucional
                PdfPTable cabecera = new PdfPTable(2);
                cabecera.WidthPercentage = 100;
                cabecera.SetWidths(new float[] { 60f, 40f });

                PdfPCell celdaIzquierda = new PdfPCell();
                celdaIzquierda.Border = Rectangle.NO_BORDER;
                celdaIzquierda.AddElement(new Paragraph("MEDISTACK CLÃNICA MÃ‰DICA", fontMarca));
                celdaIzquierda.AddElement(new Paragraph("Centro de AtenciÃ³n MÃ©dica y Especialidades Ambulatorias", fontMarcaSub));
                cabecera.AddCell(celdaIzquierda);

                PdfPCell celdaDerecha = new PdfPCell();
                celdaDerecha.Border = Rectangle.NO_BORDER;
                celdaDerecha.HorizontalAlignment = Element.ALIGN_RIGHT;
                Paragraph pTit = new Paragraph("COMPROBANTE DE COBRO", fontTituloDoc);
                pTit.Alignment = Element.ALIGN_RIGHT;
                celdaDerecha.AddElement(pTit);
                Paragraph pNum = new Paragraph("NÂ° Comprobante: #COB-" + c.CobroId.ToString("D6") + "\nFecha de emisiÃ³n: " + c.FechaHoraCobro.ToString("dd/MM/yyyy HH:mm") + " hs", fontDocMeta);
                pNum.Alignment = Element.ALIGN_RIGHT;
                celdaDerecha.AddElement(pNum);
                cabecera.AddCell(celdaDerecha);

                pdfDoc.Add(cabecera);

                // Separador
                Paragraph sep = new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, colorBorde, Element.ALIGN_CENTER, -4f)));
                sep.SpacingBefore = 8f;
                sep.SpacingAfter = 14f;
                pdfDoc.Add(sep);

                // 2) Datos del paciente y la atenciÃ³n mÃ©dica
                PdfPTable tablaDatos = new PdfPTable(2);
                tablaDatos.WidthPercentage = 100;
                tablaDatos.SetWidths(new float[] { 50f, 50f });
                tablaDatos.SpacingAfter = 14f;

                PdfPCell celdaPac = new PdfPCell();
                celdaPac.BackgroundColor = colorFondoGris;
                celdaPac.BorderColor = colorBorde;
                celdaPac.Padding = 8f;
                celdaPac.AddElement(new Paragraph("DATOS DEL PACIENTE", fontSeccion));
                Paragraph pPac = new Paragraph();
                pPac.Add(new Chunk("Paciente: ", fontEtiqueta));
                pPac.Add(new Chunk(c.PacienteNombreCompleto + "\n", fontValor));
                pPac.Add(new Chunk("DNI: ", fontEtiqueta));
                pPac.Add(new Chunk(c.PacienteDni + "\n", fontValor));
                pPac.Add(new Chunk("Obra Social: ", fontEtiqueta));
                pPac.Add(new Chunk(c.ObraSocialNombre + "\n", fontValor));
                pPac.Add(new Chunk("NÂ° Afiliado: ", fontEtiqueta));
                pPac.Add(new Chunk(c.NumeroAfiliado, fontValor));
                celdaPac.AddElement(pPac);
                tablaDatos.AddCell(celdaPac);

                PdfPCell celdaAten = new PdfPCell();
                celdaAten.BackgroundColor = colorFondoGris;
                celdaAten.BorderColor = colorBorde;
                celdaAten.Padding = 8f;
                celdaAten.AddElement(new Paragraph("DATOS DE LA ATENCIÃ“N", fontSeccion));
                Paragraph pAten = new Paragraph();
                pAten.Add(new Chunk("Profesional: ", fontEtiqueta));
                pAten.Add(new Chunk(c.ProfesionalNombreCompleto + "\n", fontValor));
                pAten.Add(new Chunk("MatrÃ­cula: ", fontEtiqueta));
                pAten.Add(new Chunk(c.ProfesionalMatricula + "\n", fontValor));
                pAten.Add(new Chunk("Especialidad: ", fontEtiqueta));
                pAten.Add(new Chunk(c.EspecialidadNombre + "\n", fontValor));
                pAten.Add(new Chunk("Turno: ", fontEtiqueta));
                pAten.Add(new Chunk("#" + c.TurnoId + " Â· " + c.FechaHoraTurno.ToString("dd/MM/yyyy HH:mm") + " hs", fontValor));
                celdaAten.AddElement(pAten);
                tablaDatos.AddCell(celdaAten);

                pdfDoc.Add(tablaDatos);

                // 3) Desglose financiero
                PdfPTable tablaDesglose = new PdfPTable(2);
                tablaDesglose.WidthPercentage = 100;
                tablaDesglose.SetWidths(new float[] { 70f, 30f });
                tablaDesglose.SpacingAfter = 14f;

                PdfPCell cabDesc = new PdfPCell(new Phrase("CONCEPTO / DETALLE", fontTablaCab));
                cabDesc.BackgroundColor = colorPrimario;
                cabDesc.BorderColor = colorPrimario;
                cabDesc.Padding = 6f;
                tablaDesglose.AddCell(cabDesc);

                PdfPCell cabImp = new PdfPCell(new Phrase("IMPORTE", fontTablaCab));
                cabImp.BackgroundColor = colorPrimario;
                cabImp.BorderColor = colorPrimario;
                cabImp.Padding = 6f;
                cabImp.HorizontalAlignment = Element.ALIGN_RIGHT;
                tablaDesglose.AddCell(cabImp);

                if (c.TipoCobro == "Consulta")
                {
                    AgregarFilaDesglose(tablaDesglose, "Valor base de la consulta mÃ©dica", "$ " + c.MontoBase.ToString("N2"), fontValor, colorBorde, false);
                    AgregarFilaDesglose(tablaDesglose, "Cobertura por Obra Social (" + c.ObraSocialNombre + ")", "- $ " + c.MontoObraSocial.ToString("N2"), fontValor, colorBorde, false);
                    AgregarFilaDesglose(tablaDesglose, "Copago arancelario a cargo del paciente", "$ " + c.Copago.ToString("N2"), fontValor, colorBorde, false);
                    if (c.SenaDescontada > 0)
                    {
                        AgregarFilaDesglose(tablaDesglose, "SeÃ±a de reserva previa descontada", "- $ " + c.SenaDescontada.ToString("N2"), fontValor, colorBorde, false);
                    }
                }
                else
                {
                    AgregarFilaDesglose(tablaDesglose, "SeÃ±a anticipada de reserva de turno", "$ " + c.MontoCobrado.ToString("N2"), fontValor, colorBorde, false);
                }

                // Fila Total
                PdfPCell celdaTotLabel = new PdfPCell(new Phrase("TOTAL ABONADO", fontTotal));
                celdaTotLabel.BackgroundColor = colorTotalFondo;
                celdaTotLabel.BorderColor = colorBorde;
                celdaTotLabel.Padding = 8f;
                tablaDesglose.AddCell(celdaTotLabel);

                PdfPCell celdaTotVal = new PdfPCell(new Phrase("$ " + c.MontoCobrado.ToString("N2"), fontTotal));
                celdaTotVal.BackgroundColor = colorTotalFondo;
                celdaTotVal.BorderColor = colorBorde;
                celdaTotVal.Padding = 8f;
                celdaTotVal.HorizontalAlignment = Element.ALIGN_RIGHT;
                tablaDesglose.AddCell(celdaTotVal);

                pdfDoc.Add(tablaDesglose);

                // 4) Medio de pago y detalle de cobro en mostrador
                PdfPTable tablaPago = new PdfPTable(2);
                tablaPago.WidthPercentage = 100;
                tablaPago.SetWidths(new float[] { 50f, 50f });
                tablaPago.SpacingAfter = 18f;

                PdfPCell celdaMedio = new PdfPCell();
                celdaMedio.BackgroundColor = colorFondoGris;
                celdaMedio.BorderColor = colorBorde;
                celdaMedio.Padding = 7f;
                Paragraph pMed = new Paragraph();
                pMed.Add(new Chunk("Medio de Pago: ", fontEtiqueta));
                pMed.Add(new Chunk(c.MedioPago + "\n", fontValor));
                if (c.MedioPago == "Efectivo")
                {
                    pMed.Add(new Chunk("Importe Recibido: ", fontEtiqueta));
                    pMed.Add(new Chunk("$ " + c.MontoRecibido.ToString("N2") + "   |   ", fontValor));
                    pMed.Add(new Chunk("Vuelto: ", fontEtiqueta));
                    pMed.Add(new Chunk("$ " + c.Vuelto.ToString("N2"), fontValor));
                }
                celdaMedio.AddElement(pMed);
                tablaPago.AddCell(celdaMedio);

                PdfPCell celdaCajero = new PdfPCell();
                celdaCajero.BackgroundColor = colorFondoGris;
                celdaCajero.BorderColor = colorBorde;
                celdaCajero.Padding = 7f;
                Paragraph pCaj = new Paragraph();
                pCaj.Add(new Chunk("Operador / Cajero: ", fontEtiqueta));
                pCaj.Add(new Chunk(c.CajeroNombreCompleto + "\n", fontValor));
                pCaj.Add(new Chunk("Estado: ", fontEtiqueta));
                pCaj.Add(new Chunk("Cobrado / Acreditado", fontValor));
                celdaCajero.AddElement(pCaj);
                tablaPago.AddCell(celdaCajero);

                pdfDoc.Add(tablaPago);

                // 5) Pie y validez
                Paragraph pie = new Paragraph("Comprobante emitido automÃ¡ticamente por el sistema de gestiÃ³n MediStack. Este documento certifica el pago registrado de la atenciÃ³n mÃ©dica correspondiente. Conserve su comprobante.", fontPie);
                pie.Alignment = Element.ALIGN_CENTER;
                pie.SpacingBefore = 10f;
                pdfDoc.Add(pie);

                pdfDoc.Close();
                return ms.ToArray();
            }
        }

        private static void AgregarFilaDesglose(PdfPTable tabla, string concepto, string monto, Font font, BaseColor colorBorde, bool esNegrita)
        {
            PdfPCell cConcepto = new PdfPCell(new Phrase(concepto, font));
            cConcepto.BorderColor = colorBorde;
            cConcepto.Padding = 6f;
            tabla.AddCell(cConcepto);

            PdfPCell cMonto = new PdfPCell(new Phrase(monto, font));
            cMonto.BorderColor = colorBorde;
            cMonto.Padding = 6f;
            cMonto.HorizontalAlignment = Element.ALIGN_RIGHT;
            tabla.AddCell(cMonto);
        }

        public static void DescargarComprobantePDF(ComprobanteCobroModel c)
        {
            byte[] bytesPdf = GenerarBytesComprobantePDF(c);
            HttpResponse response = HttpContext.Current.Response;
            response.Clear();
            response.Buffer = true;
            response.ContentType = "application/pdf";
            response.AddHeader("content-disposition", "attachment;filename=Comprobante_Cobro_" + c.CobroId + ".pdf");
            response.BinaryWrite(bytesPdf);
            response.Flush();
            response.SuppressContent = true;
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }
}