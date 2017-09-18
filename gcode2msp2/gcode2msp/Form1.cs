using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

namespace gcode2msp
{
    public partial class Form1 : Form
    {
/***     
    G1 X-0.86 Y-0.55 Z0.41 F130.516 E1.005
    G1 X-0.8 Y-0.75 Z0.41 F130.516 E1.015
    G1 X-0.7 Y-0.82 Z0.41 F130.516 E1.021
    G1 X-0.53 Y-0.84 Z0.41 F130.516 E1.029
    G1 X-0.43 Y-0.77 Z0.41 F130.516 E1.036
    G1 X0.24 Y-0.78 Z0.41 F130.516 E1.06 

    x
    ... -0.86 130.516 -8 130.516 -0.7 130.516 -0.53 130.516 -0.43 130.516 0.24 130.516 ...
        
    y
    ... -0.55 130.516 -0.75 130.516 -0.83 130.516 -0.84 130.516 -0.77 130.516 -0.78 130.516... 

    z
    .... 0.41 130.516 0.41 130.516 0.41 130.516 0.41 130.516 0.41 130.516 0.41 130.516 ...
    
***/
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult objDialogResult;
            string strFileName;
            string strPath;

            try
            {
                objDialogResult = openFileDialog1.ShowDialog();

                if (objDialogResult == DialogResult.OK)
                {    
                    textBox1.Text = openFileDialog1.FileName;

                    strFileName = Path.GetFileNameWithoutExtension(textBox1.Text);
                    strPath = Path.GetDirectoryName(textBox1.Text);

                    textBox2.Text = String.Format("{0}\\{1}.mspx", strPath, strFileName);
                    textBox3.Text = String.Format("{0}\\{1}.mspy", strPath, strFileName);
                    textBox4.Text = String.Format("{0}\\{1}.mspz", strPath, strFileName);

                    button2.Enabled = true;
                }
                else
                {
                    textBox1.Text = "";
                    textBox2.Text = "";
                    textBox3.Text = "";
                    textBox4.Text = "";

                    button2.Enabled = false;
                }
            }
            catch (Exception objException)
            {
                MessageBox.Show(objException.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string strSrc;
            string strDestX;
            string strDestY;
            string strDestZ;

            try
            {
                strSrc = textBox1.Text.Trim();
                strDestX = textBox2.Text.Trim();
                strDestY = textBox3.Text.Trim();
                strDestZ = textBox4.Text.Trim();

                if ((strSrc != "") && (strDestX != "") && (strDestY != "") && (strDestZ != ""))
                {
                    ProcessFiles(strSrc, strDestX, strDestY, strDestZ);
                }
            }
            catch (Exception objException)
            {
                MessageBox.Show(objException.Message);
            }
        }

        bool ProcessFiles(string strFileSrc, string strFileDestX, string strFileDestY, string strFileDestZ)
        {
            bool blnRslt = true;
            string strSrcLine;
            string[] strSrcValues;
            int intCount;
            string strX;
            string strY;
            string strZ;
            string strF;
            int intJ;
            int intNbParsedLine;
            int intNbFoundG1;
            float fltX;
            float fltY;
            float fltZ;
            float fltF;

            float fltPrevX;
            float fltPrevY;
            float fltPrevZ;
            float fltPrevF;

            float fltMinX;
            float fltMaxX;

            float fltMinY;
            float fltMaxY;

            float fltMinZ;
            float fltMaxZ;

            bool blnParseOK;

            try
            {
                // Vérification des fichiers Source et Destination
                if(strFileSrc=="")
                {
                    MessageBox.Show("Le fichier source n'est pas défini");
                    return false;
                }

                if (strFileDestX == "")
                {
                    MessageBox.Show("Le fichier destination axe X n'est pas défini");
                    return false;
                }

                if (strFileDestY == "")
                {
                    MessageBox.Show("Le fichier destination axe Y n'est pas défini");
                    return false;
                }

                if (strFileDestZ == "")
                {
                    MessageBox.Show("Le fichier destination axe Z n'est pas défini");
                    return false;
                }

                if (File.Exists(strFileSrc) == false)
                {
                    MessageBox.Show(String.Format("Le fichier source {0} n'est pas accessible", strFileSrc));
                    return false;
                }

                if (File.Exists(strFileDestX))
                {
                    File.Delete(strFileDestX);
                }

                if (File.Exists(strFileDestY))
                {
                    File.Delete(strFileDestY);
                }

                if (File.Exists(strFileDestZ))
                {
                    File.Delete(strFileDestZ);
                }

                // Parsing et génération des fichiers Destination

                intNbParsedLine = 0;
                intNbFoundG1 = 0;
                using (StreamReader objTextReader = new StreamReader(strFileSrc, Encoding.ASCII))
                using (StreamWriter objTextWriterX = new StreamWriter(strFileDestX, true, Encoding.ASCII))
                using (StreamWriter objTextWriterY = new StreamWriter(strFileDestY, true, Encoding.ASCII))
                using (StreamWriter objTextWriterZ = new StreamWriter(strFileDestZ, true, Encoding.ASCII))
                {
                    fltPrevX = 0.0f;
                    fltPrevY = 0.0f;
                    fltPrevZ = 0.0f;
                    fltPrevF = 0.0f;

                    fltMinX = 1000000.0f;
                    fltMaxX = 0.0f;

                    fltMinY = 1000000.0f;
                    fltMaxY = -1000000.0f;

                    fltMinZ = 1000000.0f;
                    fltMaxZ = -1000000.0f;

                    while((strSrcLine=objTextReader.ReadLine())!=null)
                    {
                        intNbParsedLine++;
                        strSrcLine = strSrcLine.Trim();

                        if (strSrcLine.StartsWith("G1 "))
                        {
                            strX = "";
                            strY = "";
                            strZ = "";
                            strF = "";

                            strSrcValues = strSrcLine.Split(' ');

                            if (strSrcValues != null)
                            {
                                intCount = strSrcValues.Length;

                                //Au moint 5 champs parsés
                                if (intCount >= 5)
                                {
                                    for (intJ = 1; intJ < intCount; intJ++)
                                    {
                                        if (strSrcValues[intJ].StartsWith("X"))
                                        {
                                            strX = strSrcValues[intJ].Substring(1);
                                        }
                                        else if (strSrcValues[intJ].StartsWith("Y"))
                                        {
                                            strY = strSrcValues[intJ].Substring(1);
                                        }
                                        else if (strSrcValues[intJ].StartsWith("Z"))
                                        {
                                            strZ = strSrcValues[intJ].Substring(1);
                                        }
                                        else if (strSrcValues[intJ].StartsWith("F"))
                                        {
                                            strF = strSrcValues[intJ].Substring(1);
                                        }
                                    }

                                    if ((strX != "") && (strY != "") && (strZ != "") && (strF != ""))
                                    {
                                        fltX = 0.0f;
                                        fltY = 0.0f;
                                        fltZ = 0.0f;
                                        fltF = 0.0f;

                                        blnParseOK = float.TryParse(strX, NumberStyles.Float, CultureInfo.InvariantCulture, out fltX);
                                        blnParseOK &= float.TryParse(strY, NumberStyles.Float, CultureInfo.InvariantCulture, out fltY);
                                        blnParseOK &= float.TryParse(strZ, NumberStyles.Float, CultureInfo.InvariantCulture, out fltZ);
                                        blnParseOK &= float.TryParse(strF, NumberStyles.Float, CultureInfo.InvariantCulture, out fltF);

                                        if ((blnParseOK) && (fltF!=0.0f))
                                        {
                                            intNbFoundG1++;
                                            //Ecriture des valeurs dans les 3 flux X Y Z

                                            strF = Math.Abs((fltX - fltPrevX) / fltF).ToString("0.00000").Replace(",",".");
                                            objTextWriterX.Write(strX);
                                            objTextWriterX.Write(" ");
                                            objTextWriterX.Write(strF);
                                            objTextWriterX.Write("\r\n");

                                            strF = Math.Abs((fltY - fltPrevY) / fltF).ToString("0.00000").Replace(",", ".");
                                            objTextWriterY.Write(strY);
                                            objTextWriterY.Write(" ");
                                            objTextWriterY.Write(strF);
                                            objTextWriterY.Write("\r\n");

                                            strF = Math.Abs(((fltZ - fltPrevZ) / fltF)).ToString("0.00000").Replace(",", ".");
                                            objTextWriterZ.Write(strZ);
                                            objTextWriterZ.Write(" ");
                                            objTextWriterZ.Write(strF);
                                            objTextWriterZ.Write("\r\n");

                                            fltPrevX = fltX;
                                            fltPrevY = fltY;
                                            fltPrevZ = fltZ;
                                            fltPrevF = fltF;

                                            if (fltX < fltMinX)
                                            {
                                                fltMinX = fltX;
                                            }

                                            if (fltX > fltMaxX)
                                            {
                                                fltMaxX = fltX;
                                            }

                                            if (fltY < fltMinY)
                                            {
                                                fltMinY = fltY;
                                            }

                                            if (fltY > fltMaxY)
                                            {
                                                fltMaxY = fltY;
                                            }


                                            if (fltZ < fltMinZ)
                                            {
                                                fltMinZ = fltZ;
                                            }
                                            
                                            if (fltZ > fltMaxZ)
                                            {
                                                fltMaxZ = fltZ;
                                            }
                                        }
                                       
                                    }                                
                                }
                            }
                        }

                        if (intNbParsedLine % 50 == 0)
                        {
                            label4.Text = String.Format("{0} lignes - {1} G1 trouvées", intNbParsedLine, intNbFoundG1);
                            label4.Refresh();           
                        }
                    }

                    objTextReader.Close();
                    objTextWriterX.Close();
                    objTextWriterY.Close();
                    objTextWriterZ.Close();
                }

                label4.Text = String.Format("{0} lignes - {1} G1 trouvées", intNbParsedLine, intNbFoundG1);
                label4.Refresh(); 
                MessageBox.Show(String.Format("Traitement terminé: {0} lignes analysées - {1} coordonnées G1 trouvées", intNbParsedLine, intNbFoundG1));

                textBox5.Text = String.Format("{0}<X<{1} \r\n{2}<Y<{3} \r\n{4}<Z<{5} \r\n",fltMinX,fltMaxX,fltMinY,fltMaxY,fltMinZ,fltMaxZ).Replace(",",".");

            }
            catch (Exception objException)
            {
                MessageBox.Show(objException.Message);
                blnRslt = false;
            }

            return blnRslt;

        }
    }
}
