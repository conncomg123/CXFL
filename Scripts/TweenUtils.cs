namespace Rendering;

using System.Numerics;
using CsXFL;
public static class TweenUtils
{
    public static Matrix4x4 CreateAffine(double a, double b, double c, double d, double tx, double ty)
    {
        return new Matrix4x4
        {
            M11 = (float)a,
            M12 = (float)b,
            M21 = (float)c,
            M22 = (float)d,
            M41 = (float)tx,
            M42 = (float)ty,
            M33 = 1,
            M44 = 1
        };
    }
    private static Matrix4x4 DeserializeMatrix(Matrix? matrix)
    {
        if (matrix is null)
        {
            return Matrix4x4.Identity;
        }

        return CreateAffine(matrix.A, matrix.B, matrix.C, matrix.D, matrix.Tx, matrix.Ty);
    }

    private static Matrix SerializeMatrix(Matrix4x4 matrix)
    {
        return new Matrix
        {
            A = matrix.M11,
            B = matrix.M12,
            C = matrix.M21,
            D = matrix.M22,
            Tx = matrix.M41,
            Ty = matrix.M42
        };
    }
    private static (double, double, double) AdjustAdobeMatrixParams(double rotation, double srot, double erot, double sshear, double eshear)
    {
        if (rotation > 0)
        {
            if (erot < srot)
            {
                erot += 2 * Math.PI;
            }
            erot += rotation * 2 * Math.PI;
        }
        else if (rotation < 0)
        {
            if (erot > srot)
            {
                erot -= 2 * Math.PI;
            }
            erot += rotation * 2 * Math.PI;
        }
        else if (Math.Abs(erot - srot) > Math.PI)
        {
            srot += Math.Sign(erot - srot) * 2 * Math.PI;
        }
        if (Math.Abs(eshear - sshear) > Math.PI)
        {
            sshear += Math.Sign(eshear - sshear) * 2 * Math.PI;
        }
        return (srot, erot, sshear);
    }
    private static (double, double, double, double) AdobeDecomposition(Matrix4x4 matrix)
    {
        double rotation = Math.Atan2(matrix[1, 0], matrix[0, 0]);
        double shear = Math.PI / 2 + rotation - Math.Atan2(matrix[1, 1], matrix[0, 1]);
        double scaleX = Math.Sqrt(matrix[0, 0] * matrix[0, 0] + matrix[1, 0] * matrix[1, 0]);
        double scaleY = Math.Sqrt(matrix[0, 1] * matrix[0, 1] + matrix[1, 1] * matrix[1, 1]);
        return (rotation, shear, scaleX, scaleY);
    }

    private static Matrix4x4 AdobeMatrix(double rotation, double shear, double scaleX, double scaleY)
    {
        var rotationMatrix = CreateAffine(Math.Cos(rotation), -Math.Sin(rotation), Math.Sin(rotation), Math.Cos(rotation), 0, 0);
        var skewMatrix = CreateAffine(1, Math.Tan(shear), 0, 1, 0, 0);
        var scaleMatrix = CreateAffine(scaleX, 0, 0, scaleY * Math.Cos(shear), 0, 0);
        return rotationMatrix * skewMatrix * scaleMatrix;
    }
    public static Matrix SimpleMatrixInterpolation(Matrix start, Matrix end, double t)
    {
        var startMatrix = DeserializeMatrix(start);
        var endMatrix = DeserializeMatrix(end);
        (var srot, var sshear, var sx, var sy) = AdobeDecomposition(startMatrix);
        (var erot, var eshear, var ex, var ey) = AdobeDecomposition(endMatrix);
        (srot, erot, sshear) = AdjustAdobeMatrixParams(0, srot, erot, sshear, eshear);
        var interpolatedLinear = AdobeMatrix(
            t * (erot) + (1 - t) * srot,
            t * eshear + (1 - t) * sshear,
            t * ex + (1 - t) * sx,
            t * ey + (1 - t) * sy
        );
        interpolatedLinear.M41 = (float)(startMatrix.M41 * (1 - t) + endMatrix.M41 * t);
        interpolatedLinear.M42 = (float)(startMatrix.M42 * (1 - t) + endMatrix.M42 * t);
        return SerializeMatrix(interpolatedLinear);
    }
    public static Matrix MatrixInterpolation(Matrix start, Matrix end, double rotation, Frame tweenFrame, int frameIndex, Point tp)
    {
        var startMatrix = DeserializeMatrix(start);
        var endMatrix = DeserializeMatrix(end);
        (var srot, var sshear, var sx, var sy) = AdobeDecomposition(startMatrix);
        (var erot, var eshear, var ex, var ey) = AdobeDecomposition(endMatrix);
        (srot, erot, sshear) = AdjustAdobeMatrixParams(rotation, srot, erot, sshear, eshear);
        var frot = tweenFrame.GetTweenMultiplier(frameIndex, "rotation");
        var fscale = tweenFrame.GetTweenMultiplier(frameIndex, "scale");
        var fpos = tweenFrame.GetTweenMultiplier(frameIndex, "position");
        var interpolatedLinear = AdobeMatrix(
            frot * erot + (1 - frot) * srot,
            frot * eshear + (1 - frot) * sshear,
            fscale * ex + (1 - fscale) * sx,
            fscale * ey + (1 - fscale) * sy
        );
        Matrix4x4 transformOrigin = Matrix4x4.Identity;
        transformOrigin.M41 = (float)-tp.X;
        transformOrigin.M42 = (float)-tp.Y;
        transformOrigin *= interpolatedLinear;
        transformOrigin.M41 += (float)tp.X;
        transformOrigin.M42 += (float)tp.Y;
        interpolatedLinear.M41 += (float)(startMatrix.M41 * (1 - fpos) + endMatrix.M41 * fpos + transformOrigin.M41);
        interpolatedLinear.M42 += (float)(startMatrix.M42 * (1 - fpos) + endMatrix.M42 * fpos + transformOrigin.M42);
        return SerializeMatrix(interpolatedLinear);
    }
    private static Color Scale(this Color color, double factor)
    {
        List<(double Red, double Green, double Blue, double Alpha)> multipliers = ColorEffectUtils.GetMultipliers(color);
        Color cpy = Color.DefaultColor();
        cpy.RedMultiplier = multipliers[0].Red * factor;
        cpy.GreenMultiplier = multipliers[0].Green * factor;
        cpy.BlueMultiplier = multipliers[0].Blue * factor;
        cpy.AlphaMultiplier = multipliers[0].Alpha * factor;
        cpy.RedOffset = (int)(multipliers[1].Red * factor);
        cpy.GreenOffset = (int)(multipliers[1].Green * factor);
        cpy.BlueOffset = (int)(multipliers[1].Blue * factor);
        cpy.AlphaOffset = (int)(multipliers[1].Alpha * factor);
        return cpy;
    }
    private static Color Add(this Color color, Color other)
    {
        List<(double Red, double Green, double Blue, double Alpha)> theseMultipliers = ColorEffectUtils.GetMultipliers(color);
        List<(double Red, double Green, double Blue, double Alpha)> otherMultipliers = ColorEffectUtils.GetMultipliers(other);
        Color cpy = Color.DefaultColor();
        cpy.RedMultiplier = theseMultipliers[0].Red + otherMultipliers[0].Red;
        cpy.GreenMultiplier = theseMultipliers[0].Green + otherMultipliers[0].Green;
        cpy.BlueMultiplier = theseMultipliers[0].Blue + otherMultipliers[0].Blue;
        cpy.AlphaMultiplier = theseMultipliers[0].Alpha + otherMultipliers[0].Alpha;
        cpy.RedOffset = (int)(theseMultipliers[1].Red + otherMultipliers[1].Red);
        cpy.GreenOffset = (int)(theseMultipliers[1].Green + otherMultipliers[1].Green);
        cpy.BlueOffset = (int)(theseMultipliers[1].Blue + otherMultipliers[1].Blue);
        cpy.AlphaOffset = (int)(theseMultipliers[1].Alpha + otherMultipliers[1].Alpha);
        return cpy;
    }
    public static Color ColorInterpolation(Color? start, Color? end, Frame tweenFrame, int frameIndex)
    {
        start ??= Color.DefaultColor();
        end ??= Color.DefaultColor();
        var frac = tweenFrame.GetTweenMultiplier(frameIndex, "color");
        return end.Scale(frac).Add(start.Scale(1 - frac));
    }
}