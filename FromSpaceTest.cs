using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AtmosphericScatteringTest
{
    public partial class FromSpaceTest : Form
    {
        public FromSpaceTest()
        {
            InitializeComponent();

            nmrCamPosZ.Value = 500;

            nmrLightPosX.Value = 1000;
            nmrLightPosY.Value = 1000;
            nmrLightPosZ.Value = 1000;

            nmrESun.Value = 15;

            nmrG.Value = -0.980M;

            nmrWavelengthR.Value = 0.731M;
            nmrWavelengthG.Value = 0.612M;
            nmrWavelengthB.Value = 0.455M;

            nmrSamples.Value = 3M;

            nmrKr.Value = 0.0025M;
            nmrKm.Value = 0.0015M;
            nmrRatio.Value = 1.025M;
            nmrScaleDepth.Value = 0.25M;

            pnlAtmosphere.BackColor = Color.Black;
            pnlGround.BackColor = Color.Black;

        }

       

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            btnGenerate.Enabled = false;
            Vector3 v3CamPos = new Vector3((float)nmrCamPosX.Value, (float)nmrCamPosY.Value, (float)nmrCamPosZ.Value);
            Vector3 v3LightPos = new Vector3((float)nmrLightPosX.Value, (float)nmrLightPosY.Value, (float)nmrLightPosZ.Value);
            Vector3 wavelength = new Vector3((float)nmrWavelengthR.Value, (float)nmrWavelengthG.Value, (float)nmrWavelengthB.Value);
            int samples = (int)nmrSamples.Value;
            v3LightPos.Normalize();

            float Kr = (float)nmrKr.Value;
            float Km = (float)nmrKm.Value;
            float ESun = (float)nmrESun.Value;

            float G = (float)nmrG.Value;

            float radiusRatio = (float)nmrRatio.Value;
            float fScaleDepth = (float)nmrScaleDepth.Value;

            
            pnlAtmosphere.BackgroundImage = CreateSkyFromSpace(200, samples, radiusRatio, fScaleDepth, Kr, Km, ESun, G, v3CamPos, Vector3.Zero, v3LightPos, wavelength);
            pnlGround.BackgroundImage = CreateGroundFromSpace(200, samples, radiusRatio, fScaleDepth, Kr, Km, ESun, G, v3CamPos, Vector3.Zero, v3LightPos, wavelength);
            btnGenerate.Enabled = true;
        }
        protected Bitmap CreateSkyFromSpace(float radius, int samples, float radiusRatio, float fScaleDepth, float Kr, float Km, float ESun, float G, Vector3 cameraPosition, Vector3 planetPosition, Vector3 lightPosition, Vector3 wavelength)
        {

            float fInnerRadius = radius / radiusRatio;     // The inner (planetary) radius
            float fInnerRadius2 = fInnerRadius * fInnerRadius;    // fInnerRadius^2
            float fOuterRadius = radius;     // The outer (atmosphere) radius
            float fOuterRadius2 = fOuterRadius * fOuterRadius;    // fOuterRadius^2

            Vector3 v3CameraPos = cameraPosition;       // The camera's current position
            Vector3 v3LightPos = Vector3.Normalize(lightPosition);        // The Light Position
            Vector3 v3InvWavelength = new Vector3(1 / (float)Math.Pow(wavelength.X, 4), 1 / (float)Math.Pow(wavelength.Y, 4), 1 / (float)Math.Pow(wavelength.Z, 4));   // 1 / pow(wavelength, 4) for the red, green, and blue channels

            float fKrESun = Kr * ESun;         // Kr * ESun
            float fKmESun = Km * ESun;          // Km * ESun
            float fKr4PI = Kr * 4.0f * (float)Math.PI;           // Kr * 4 * PI
            float fKm4PI = Km * 4.0f * (float)Math.PI; ;           // Km * 4 * PI
            float fScale = 1 / (fOuterRadius - fInnerRadius);           // 1 / (fOuterRadius - fInnerRadius)
            float fScaleOverScaleDepth = fScale / fScaleDepth; // fScale / fScaleDepth
            int nSamples = samples;

            float fCameraHeight = (v3CameraPos).Length();    // The camera's current height
            float fCameraHeight2 = fCameraHeight * fCameraHeight;   // fCameraHeight^2

            float fg = G;
            float fg2 = G * G;

            float fSamples = (float)nSamples;

            int width = (int)Math.Ceiling(fOuterRadius);
            int height = (int)Math.Ceiling(fOuterRadius);
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width * 2, height * 2);
            for (int iy = 0; iy < height * 2; iy++)
            {
                for (int ix = 0; ix < width * 2; ix++)
                {
                    // Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
                    float k = 1 - ((float)(ix - radius) * (float)(ix - radius)) / (radius * radius) - ((float)(iy - radius) * (float)(iy - radius)) / (radius * radius);

                    if (k <= 0) continue;

                    Vector3 v3Pos = new Vector3(ix - width, iy - height, (float)Math.Sqrt(k) * radius);
                    Vector3 v3Ray = v3Pos - v3CameraPos;
                    float fFar = v3Ray.Length();
                    v3Ray /= fFar;

                    // Calculate the closest intersection of the ray with the outer atmosphere (which is the near point of the ray passing through the atmosphere)
                    float B = 2.0f * Vector3.Dot(v3CameraPos, v3Ray);
                    float C = fCameraHeight2 - fOuterRadius2;
                    float fDet = Math.Max(0.0f, B * B - 4.0f * C);
                    float fNear = 0.5f * (-B - (float)Math.Sqrt(fDet));

                    // Calculate the ray's starting position, then calculate its scattering offset
                    Vector3 v3Start = v3CameraPos + v3Ray * fNear;
                    fFar -= fNear;
                    float fStartAngle = Vector3.Dot(v3Ray, v3Start) / fOuterRadius;
                    float fStartDepth = (float)Math.Exp(-1.0 / fScaleDepth);
                    float fStartOffset = fStartDepth * Scale(fStartAngle, fScaleDepth);

                    // Initialize the scattering loop variables
                    float fSampleLength = fFar / fSamples;
                    float fScaledLength = fSampleLength * fScale;
                    Vector3 v3SampleRay = v3Ray * fSampleLength;
                    Vector3 v3SamplePoint = v3Start + v3SampleRay * 0.5f;
                    // Now loop through the sample rays
                    Vector3 v3FrontColor = new Vector3();
                    for (int i = 0; i < nSamples; i++)
                    {
                        float fHeight = v3SamplePoint.Length();
                        float fDepth = (float)Math.Exp(fScaleOverScaleDepth * (fInnerRadius - fHeight));
                        float fLightAngle = Vector3.Dot(v3LightPos, v3SamplePoint) / fHeight;
                        float fCameraAngle = Vector3.Dot(v3Ray, v3SamplePoint) / fHeight;
                        fCameraAngle = 1;
                        float fScatter = (fStartOffset + fDepth * (Scale(fLightAngle, fScaleDepth) - Scale(fCameraAngle, fScaleDepth)));
                        Vector3 v3Attenuate = Exp((v3InvWavelength * fKr4PI + new Vector3(fKm4PI)) * -fScatter);
                        v3FrontColor += v3Attenuate * (fDepth * fScaledLength);
                        v3SamplePoint += v3SampleRay;
                    }

                    // Finally, scale the Mie and Rayleigh colors and set up the varying variables for the pixel shader
                    Vector3 vMieColor = v3FrontColor * fKmESun;
                    Vector3 vRayleighColor = v3FrontColor * (v3InvWavelength * fKrESun);

                    Vector3 v3Direction = v3CameraPos - v3Pos;
                    float fCos = Vector3.Dot(v3LightPos, v3Direction) / v3Direction.Length();
                    float fCos2 = fCos * fCos;
                    float fRayleighPhase = 0.75f * (1.0f + fCos2);
                    float fMiePhase = 1.5f * ((1.0f - fg2) / (2.0f + fg2)) * (1.0f + fCos2) / (float)Math.Pow(1.0f + fg2 - 2.0f * fg * fCos, 1.5f);
                    Vector3 color = fRayleighPhase * vRayleighColor + fMiePhase * vMieColor;

                    bitmap.SetPixel(ix, iy, FromRGBA(color.X, color.Y, color.Z, color.Z));

                }

            }


            return bitmap;

        }




        protected Bitmap CreateGroundFromSpace(float radius, int samples, float radiusRatio, float fScaleDepth, float Kr, float Km, float ESun, float G, Vector3 cameraPosition, Vector3 planetPosition, Vector3 lightPosition, Vector3 wavelength)
        {
            float fInnerRadius = radius / radiusRatio;     // The inner (planetary) radius
            float fInnerRadius2 = fInnerRadius * fInnerRadius;    // fInnerRadius^2
            float fOuterRadius = radius;     // The outer (atmosphere) radius
            float fOuterRadius2 = fOuterRadius * fOuterRadius;    // fOuterRadius^2

            Vector3 v3CameraPos = cameraPosition;       // The camera's current position
            Vector3 v3LightPos = Vector3.Normalize(lightPosition);        // The Light Position
            Vector3 v3InvWavelength = new Vector3(1 / (float)Math.Pow(wavelength.X, 4), 1 / (float)Math.Pow(wavelength.Y, 4), 1 / (float)Math.Pow(wavelength.Z, 4));   // 1 / pow(wavelength, 4) for the red, green, and blue channels

            float fExposure = -2;
            float fKrESun = Kr * ESun;         // Kr * ESun
            float fKmESun = Km * ESun;          // Km * ESun
            float fKr4PI = Kr * 4.0f * (float)Math.PI;           // Kr * 4 * PI
            float fKm4PI = Km * 4.0f * (float)Math.PI; ;           // Km * 4 * PI
            float fScale = 1 / (fOuterRadius - fInnerRadius);           // 1 / (fOuterRadius - fInnerRadius)
            float fScaleOverScaleDepth = fScale / fScaleDepth; // fScale / fScaleDepth
            int nSamples = 5;

            float fCameraHeight = (v3CameraPos).Length();    // The camera's current height
            float fCameraHeight2 = fCameraHeight * fCameraHeight;   // fCameraHeight^2

            float fg = G;
            float fg2 = G * G;

            float fInvScaleDepth = (1.0f / fScaleDepth);

            float fSamples = (float)nSamples;


            int width = (int)Math.Ceiling(fOuterRadius);
            int height = (int)Math.Ceiling(fOuterRadius);
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width * 2, height * 2);
            for (int iy = 0; iy < height * 2; iy++)
            {
                for (int ix = 0; ix < width * 2; ix++)
                {
                    // Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
                    float k = 1 - ((float)(ix - radius) * (float)(ix - radius)) / (radius * radius) - ((float)(iy - radius) * (float)(iy - radius)) / (radius * radius);

                    if (k <= 0) continue;

                    Vector3 v3Pos = new Vector3(ix - width, iy - height, (float)Math.Sqrt(k) * radius);
                    Vector3 v3Ray = v3Pos - v3CameraPos;
                    float fFar = v3Ray.Length();
                    v3Ray /= fFar;

                    // Calculate the closest intersection of the ray with the outer atmosphere (which is the near point of the ray passing through the atmosphere)
                    float B = 2.0f * Vector3.Dot(v3CameraPos, v3Ray);
                    float C = fCameraHeight2 - fOuterRadius2;
                    float fDet = Math.Max(0.0f, B * B - 4.0f * C);
                    float fNear = 0.5f * (-B - (float)Math.Sqrt(fDet));

                    // Calculate the ray's starting position, then calculate its scattering offset
                    Vector3 v3Start = v3CameraPos + v3Ray * fNear;
                    fFar -= fNear;
                    float fDepth = (float)Math.Exp((fInnerRadius - fOuterRadius) / fScaleDepth);
                    float fCameraAngle = Vector3.Dot(-v3Ray, v3Pos) / v3Pos.Length();
                    float fLightAngle = Vector3.Dot(v3LightPos, v3Pos) / v3Pos.Length();
                    float fCameraScale = Scale(fCameraAngle, fScaleDepth);
                    float fLightScale = Scale(fLightAngle, fScaleDepth);
                    float fCameraOffset = fDepth * fCameraScale;
                    float fTemp = (fLightScale + fCameraScale);

                    // Initialize the scattering loop variables
                    float fSampleLength = fFar / fSamples;
                    float fScaledLength = fSampleLength * fScale;
                    Vector3 v3SampleRay = v3Ray * fSampleLength;
                    Vector3 v3SamplePoint = v3Start + v3SampleRay * 0.5f;

                    // Now loop through the sample rays
                    Vector3 v3FrontColor = new Vector3(0);
                    Vector3 v3Attenuate = new Vector3(0);
                    for (int i = 0; i < nSamples; i++)
                    {
                        float fHeight = v3SamplePoint.Length();

                        float fSampleDepth = (float)Math.Exp(fScaleOverScaleDepth * (fInnerRadius - fHeight));
                        float fScatter = fSampleDepth * fTemp - fCameraOffset;

                        Vector3 expComponent = (v3InvWavelength * fKr4PI + new Vector3(fKm4PI)) * -fScatter;
                        v3Attenuate = Exp(expComponent);
                        v3FrontColor += v3Attenuate * (fSampleDepth * fScaledLength);
                        v3SamplePoint += v3SampleRay;
                    }

                    // scattering colors
                    Vector3 vRayleighColor = v3FrontColor * (v3InvWavelength * fKrESun + new Vector3(fKmESun));
                    Vector3 vMieColor = v3Attenuate;
                    Vector3 color = vRayleighColor + new Vector3(0.25f, 0.25f, 0.3f) * vMieColor;

                    bitmap.SetPixel(ix, iy, FromRGBA(color.X, color.Y, color.Z, 1));
                }

            }


            return bitmap;

        }


        // Returns the near intersection point of a line and a sphere
        float GetNearIntersection(ref Vector3 position, ref Vector3 ray, float distanceSquared, float radiusSquared)
        {
            float B = Vector3.Dot(position, ray) * 2;

            float C = distanceSquared - radiusSquared;
            double fDet = Math.Max(0.0, B * B - 4.0 * C);
            return 0.5f * (-B - (float)Math.Sqrt(fDet));
        }

        float Scale(float fCos, float fScaleDepth)
        {
            float x = 1.0f - fCos;
            return fScaleDepth * (float)Math.Exp(-0.00287 + x * (0.459 + x * (3.83 + x * (-6.80 + x * 5.25))));
        }



        Color FromRGBA(float red, float green, float blue, float alpha)
        {
            if(float.IsInfinity(red) ||float.IsInfinity(green) ||float.IsInfinity(blue) ||float.IsInfinity(alpha)) {
                //return Color.Purple;
                return Color.Transparent;
            }
            if (float.IsNaN(red) || float.IsNaN(green) || float.IsNaN(blue) || float.IsNaN(alpha))
            {
                //return Color.Pink;
                return Color.Transparent;
            }




            red = MathHelper.Clamp(red, 0, 1);
            green = MathHelper.Clamp(green, 0, 1);
            blue = MathHelper.Clamp(blue, 0, 1);
            alpha = MathHelper.Clamp(alpha, 0, 1);


            return Color.FromArgb((int)(alpha * 255), (int)(red * 255), (int)(green * 255), (int)(blue * 255));
        }

        Vector3 Exp(Vector3 v)
        {
            return new Vector3((float)Math.Exp(v.X), (float)Math.Exp(v.Y), (float)Math.Exp(v.Z));
        }
    }


}
