using DPFP;
using DPFP.Capture;
using DPFP.Processing;
using System.Drawing;
using System;

namespace BiomentricoHolding.Services
{
    public class CapturaHuellaService : DPFP.Capture.EventHandler
    {
        private Capture Capturador;
        private Enrollment Enroller;

        public Template TemplateCapturado { get; private set; }

        public event Action<string> Mensaje;
        public event Action<Template> TemplateGenerado;
        public event Action<System.Drawing.Bitmap> MuestraProcesada;

        public CapturaHuellaService()
        {
            try
            {
                Capturador = new Capture();
                if (Capturador != null)
                {
                    Capturador.EventHandler = this;
                    Enroller = new Enrollment();
                }
                else
                {
                    Mensaje?.Invoke("⚠ No se pudo inicializar el capturador.");
                }
            }
            catch (Exception ex)
            {
                Mensaje?.Invoke($"❌ Error al inicializar lector: {ex.Message}");
            }
        }

        public void IniciarCaptura()
        {
            try
            {
                Capturador?.StartCapture();
                Mensaje?.Invoke("Coloca tu dedo en el lector.");
            }
            catch (Exception ex)
            {
                Mensaje?.Invoke($"❌ Error al iniciar la captura: {ex.Message}");
            }
        }

        public void DetenerCaptura()
        {
            try
            {
                Capturador?.StopCapture();
                Mensaje?.Invoke("Captura detenida.");
            }
            catch (Exception ex)
            {
                Mensaje?.Invoke($"❌ Error al detener la captura: {ex.Message}");
            }
        }

        public void OnComplete(object capture, string readerSerialNumber, Sample sample)
        {
            var features = ExtractFeatures(sample, DPFP.Processing.DataPurpose.Enrollment);

            if (features != null)
            {
                try
                {
                    Enroller.AddFeatures(features);
                    Mensaje?.Invoke($"✅ Muestra válida. Faltan {Enroller.FeaturesNeeded} muestra(s).");

                    var img = ConvertirMuestraAImagen(sample);
                    MuestraProcesada?.Invoke(img);

                    if (Enroller.TemplateStatus == DPFP.Processing.Enrollment.Status.Ready)
                    {
                        TemplateCapturado = Enroller.Template;
                        TemplateGenerado?.Invoke(TemplateCapturado);
                        Mensaje?.Invoke("✅ Huella lista para guardar.");
                        DetenerCaptura();
                    }
                    else if (Enroller.TemplateStatus == DPFP.Processing.Enrollment.Status.Failed)
                    {
                        Mensaje?.Invoke("❌ Las muestras no coinciden. Reiniciando...");
                        Enroller.Clear();
                        IniciarCaptura();
                    }
                }
                catch (DPFP.Error.SDKException ex)
                {
                    Mensaje?.Invoke("❌ Error al capturar huella: " + ex.Message);
                    Enroller.Clear();
                    IniciarCaptura();
                }
            }
            else
            {
                Mensaje?.Invoke("⚠ Huella no clara. Intenta nuevamente.");
            }
        }

        private System.Drawing.Bitmap ConvertirMuestraAImagen(Sample sample)
        {
            var convertidor = new DPFP.Capture.SampleConversion();
            var imagen = new System.Drawing.Bitmap(100, 100);
            convertidor.ConvertToPicture(sample, ref imagen);
            return imagen;
        }

        public void OnFingerTouch(object capture, string readerSerialNumber)
        {
            Mensaje?.Invoke("Dedo detectado...");
        }

        public void OnFingerGone(object capture, string readerSerialNumber)
        {
            Mensaje?.Invoke("Dedo retirado.");
        }

        public void OnReaderConnect(object capture, string readerSerialNumber)
        {
            Mensaje?.Invoke("Lector conectado.");
        }

        public void OnReaderDisconnect(object capture, string readerSerialNumber)
        {
            Mensaje?.Invoke("Lector desconectado.");
        }

        public void OnSampleQuality(object capture, string readerSerialNumber, CaptureFeedback feedback)
        {
            if (feedback == CaptureFeedback.Good)
                Mensaje?.Invoke("👌 Calidad de huella aceptable.");
            else
                Mensaje?.Invoke("⚠ Huella no suficientemente clara.");
        }

        private FeatureSet ExtractFeatures(Sample sample, DPFP.Processing.DataPurpose purpose)
        {
            var extractor = new DPFP.Processing.FeatureExtraction();
            CaptureFeedback feedback = CaptureFeedback.None;
            var features = new FeatureSet();

            extractor.CreateFeatureSet(sample, purpose, ref feedback, ref features);

            return (feedback == CaptureFeedback.Good) ? features : null;
        }
    }
}
