using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    public static class CoreLightEditorUtilities
    {
        private static Color GetLightHandleColor(Color wireframeColor)
        {
            Color color = wireframeColor;
            color.a = Mathf.Clamp01(color.a * 2);
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color.linear : color;
        }

        // Don't use Handles.Disc as it break the highlight of the gizmo axis, use our own draw disc function instead for gizmo
        public static void DrawWireDisc(Quaternion q, Vector3 position, Vector3 axis, float radius)
        {
            Matrix4x4 rotation = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);
            
            float theta = 0.0f;
            float x = radius * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(theta);
            Vector3 pos = rotation * new Vector3(x, y, 0);
            pos += position;
            Vector3 newPos = pos;
            Vector3 lastPos = pos;
            for (theta = 0.1f; theta < 2.0f * Mathf.PI; theta += 0.1f)
            {
                x = radius * Mathf.Cos(theta);
                y = radius * Mathf.Sin(theta);

                newPos = rotation * new Vector3(x, y, 0);
                newPos += position;
                Gizmos.DrawLine(pos, newPos);
                pos = newPos;
            }
            Gizmos.DrawLine(pos, lastPos);
        }

        public static float SliderHandle(Vector3 position, Vector3 direction, float value)
        {
            Vector3 pos = position + direction * value;
            float sizeHandle = HandleUtility.GetHandleSize(pos);
            bool temp = GUI.changed;
            GUI.changed = false;
            position = Handles.Slider(pos, direction, sizeHandle * 0.03f, Handles.DotHandleCap, 0f);
            if (GUI.changed)
                value = Vector3.Dot(position - position, direction);
            GUI.changed |= temp;
            return value;
        }


        // innerSpotPercent - 0 to 1 value (percentage 0 - 100%)
        public static void DrawSpotlightHandle(Light spotlight, float innerSpotPercent, bool selected)
        {
            var flatRadiusAtRange = spotlight.range * Mathf.Tan(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);

            var vectorLineUp = Vector3.Normalize(spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * flatRadiusAtRange);
            var vectorLineDown = Vector3.Normalize(spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * -flatRadiusAtRange);
            var vectorLineRight = Vector3.Normalize(spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * flatRadiusAtRange);
            var vectorLineLeft = Vector3.Normalize(spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * -flatRadiusAtRange);

            var rangeDiscDistance = Mathf.Cos(Mathf.Deg2Rad * spotlight.spotAngle / 2) * spotlight.range;
            var rangeDiscRadius = spotlight.range * Mathf.Sin(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);
            var nearDiscDistance = Mathf.Cos(Mathf.Deg2Rad * spotlight.spotAngle / 2) * spotlight.shadowNearPlane;
            var nearDiscRadius = spotlight.shadowNearPlane * Mathf.Sin(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);
            
            using (new Handles.DrawingScope(Gizmos.color))
            {
                DrawCone(spotlight.gameObject.transform.position, spotlight.gameObject.transform.rotation, spotlight.range, spotlight.spotAngle * innerSpotPercent);

                if (selected)
                {
                    //Inner Cone
                    DrawCone(spotlight.gameObject.transform.position, spotlight.gameObject.transform.rotation, spotlight.range, spotlight.spotAngle);

                    //Draw Range Arcs
                    Handles.DrawWireArc(spotlight.gameObject.transform.position, spotlight.gameObject.transform.right, vectorLineUp, spotlight.spotAngle, spotlight.range);
                    Handles.DrawWireArc(spotlight.gameObject.transform.position, spotlight.gameObject.transform.up, vectorLineLeft, spotlight.spotAngle, spotlight.range);

                    //Draw Near Plane Disc
                    if (spotlight.shadows != LightShadows.None)
                        Handles.DrawWireDisc(spotlight.gameObject.transform.forward, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * nearDiscDistance, nearDiscRadius);

                    //waiting for Martin handles
                }
            }
        }

        public static void DrawCone(Vector3 position, Quaternion rotation, float range, float angle)
        {
            var flatRadiusAtRange = range * Mathf.Tan(angle * Mathf.Deg2Rad * 0.5f);
            var forward = rotation * Vector3.forward;
            var up = rotation * Vector3.up;
            var right = rotation * Vector3.right;

            var vectorLineUp = Vector3.Normalize(forward * range + up * flatRadiusAtRange);
            var vectorLineDown = Vector3.Normalize(forward * range + up * -flatRadiusAtRange);
            var vectorLineRight = Vector3.Normalize(forward * range + right * flatRadiusAtRange);
            var vectorLineLeft = Vector3.Normalize(forward * range + right * -flatRadiusAtRange);

            //Draw Lines
            Handles.DrawLine(position, position + vectorLineUp * range);
            Handles.DrawLine(position, position + vectorLineDown * range);
            Handles.DrawLine(position, position + vectorLineRight * range);
            Handles.DrawLine(position, position + vectorLineLeft * range);
            
            if (angle > 0)
            {
                var discDistance = Mathf.Cos(Mathf.Deg2Rad * angle * 0.5f) * range;
                var discRadius = range * Mathf.Sin(angle * Mathf.Deg2Rad * 0.5f);
                Handles.DrawWireDisc(position + forward * discDistance, forward, discRadius);
            }
        }

        public static void DrawArealightGizmo(Light arealight)
        {
            var RectangleSize = new Vector3(arealight.areaSize.x, arealight.areaSize.y, 0);
            // Remove scale for light, not take into account
            var localToWorldMatrix = Matrix4x4.TRS(arealight.transform.position, arealight.transform.rotation, Vector3.one);
            Gizmos.matrix = localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, RectangleSize);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireSphere(arealight.transform.position, arealight.range);
        }

        [Obsolete("Should use the legacy gizmo draw")]
        public static void DrawPointlightGizmo(Light pointlight, bool selected)
        {
            if (pointlight.shadows != LightShadows.None && selected) Gizmos.DrawWireSphere(pointlight.transform.position, pointlight.shadowNearPlane);
            Gizmos.DrawWireSphere(pointlight.transform.position, pointlight.range);
        }

        // Same as Gizmo.DrawFrustum except that when aspect is below one, fov represent fovX instead of fovY
        // Use to match our light frustum pyramid behavior
        public static void DrawLightPyramidFrustumHandle(Vector3 center, float fov, ref float maxRange, ref float minRange, ref float aspect)
        {
            fov = Mathf.Deg2Rad * fov * 0.5f;
            float tanfov = Mathf.Tan(fov);
            Vector3 farEnd = new Vector3(0, 0, maxRange);
            Vector3 endSizeX;
            Vector3 endSizeY;

            if (aspect >= 1.0f)
            {
                endSizeX = new Vector3(maxRange * tanfov * aspect, 0, 0);
                endSizeY = new Vector3(0, maxRange * tanfov, 0);
            }
            else
            {
                endSizeX = new Vector3(maxRange * tanfov, 0, 0);
                endSizeY = new Vector3(0, maxRange * tanfov / aspect, 0);
            }

            Vector3 s1, s2, s3, s4;
            Vector3 e1 = farEnd + endSizeX + endSizeY;
            Vector3 e2 = farEnd - endSizeX + endSizeY;
            Vector3 e3 = farEnd - endSizeX - endSizeY;
            Vector3 e4 = farEnd + endSizeX - endSizeY;
            if (minRange <= 0.0f)
            {
                s1 = s2 = s3 = s4 = center;
            }
            else
            {
                Vector3 startSizeX;
                Vector3 startSizeY;
                if (aspect >= 1.0f)
                {
                    startSizeX = new Vector3(minRange * tanfov * aspect, 0, 0);
                    startSizeY = new Vector3(0, minRange * tanfov, 0);
                }
                else
                {
                    startSizeY = new Vector3(minRange * tanfov / aspect, 0, 0);
                    startSizeX = new Vector3(0, minRange * tanfov, 0);
                }
                Vector3 startPoint = center;
                s1 =    startPoint + startSizeX + startSizeY;
                s2 =    startPoint - startSizeX + startSizeY;
                s3 =    startPoint - startSizeX - startSizeY;
                s4 =    startPoint + startSizeX - startSizeY;
                Handles.DrawLine(s1, s2);
                Handles.DrawLine(s2, s3);
                Handles.DrawLine(s3, s4);
                Handles.DrawLine(s4, s1);
            }

            Handles.DrawLine(e1, e2);
            Handles.DrawLine(e2, e3);
            Handles.DrawLine(e3, e4);
            Handles.DrawLine(e4, e1);

            Handles.DrawLine(s1, e1);
            Handles.DrawLine(s2, e2);
            Handles.DrawLine(s3, e3);
            Handles.DrawLine(s4, e4);




            Handles.color = GetLightHandleColor(Handles.color);

            if(minRange > 0f)
            {
                float x = (s1.x - s2.x) * 0.5f;
                float y = (s1.x - s2.x) * 0.5f;
                x = SliderHandle(center, Vector3.right, x);
                x = SliderHandle(center, Vector3.left, x);
                y = SliderHandle(center, Vector3.up, y);
                y = SliderHandle(center, Vector3.down, y);

            }

            //draw max handles
            float halfWidth = 0.5f * size.x;
            float halfHeight = 0.5f * size.y;

            if (!handlesOnly)
            {
                Vector3 topRight = position + Vector3.up * halfHeight + Vector3.right * halfWidth;
                Vector3 bottomRight = position - Vector3.up * halfHeight + Vector3.right * halfWidth;
                Vector3 bottomLeft = position - Vector3.up * halfHeight - Vector3.right * halfWidth;
                Vector3 topLeft = position + Vector3.up * halfHeight - Vector3.right * halfWidth;

                // Draw rectangle
                DrawLine(topRight, bottomRight);
                DrawLine(bottomRight, bottomLeft);
                DrawLine(bottomLeft, topLeft);
                DrawLine(topLeft, topRight);
            }

            // Give handles twice the alpha of the lines
            Color origCol = color;
            Color col = color;
            col.a = Mathf.Clamp01(color.a * 2);
            color = ToActiveColorSpace(col);

            // Draw handles
            halfHeight = SizeSlider(position, up, halfHeight);
            halfHeight = SizeSlider(position, -up, halfHeight);
            halfWidth = SizeSlider(position, right, halfWidth);
            halfWidth = SizeSlider(position, -right, halfWidth);

            size.x = Mathf.Max(0f, 2.0f * halfWidth);
            size.y = Mathf.Max(0f, 2.0f * halfHeight);

            color = origCol;

        }

        public static void DrawLightOrthoFrustum(Vector3 center, float width, float height, float maxRange, float minRange)
        {
            Vector3 farEnd = new Vector3(0, 0, maxRange);
            Vector3 endSizeX = new Vector3(width, 0, 0);
            Vector3 endSizeY = new Vector3(0, height, 0);

            Vector3 s1, s2, s3, s4;
            Vector3 e1 = farEnd + endSizeX + endSizeY;
            Vector3 e2 = farEnd - endSizeX + endSizeY;
            Vector3 e3 = farEnd - endSizeX - endSizeY;
            Vector3 e4 = farEnd + endSizeX - endSizeY;
            if (minRange <= 0.0f)
            {
                s1 = s2 = s3 = s4 = center;
            }
            else
            {
                Vector3 startSizeX = new Vector3(width, 0, 0);
                Vector3 startSizeY = new Vector3(0, height, 0);

                Vector3 startPoint = center;
                s1 =    startPoint + startSizeX + startSizeY;
                s2 =    startPoint - startSizeX + startSizeY;
                s3 =    startPoint - startSizeX - startSizeY;
                s4 =    startPoint + startSizeX - startSizeY;
                Gizmos.DrawLine(s1, s2);
                Gizmos.DrawLine(s2, s3);
                Gizmos.DrawLine(s3, s4);
                Gizmos.DrawLine(s4, s1);
            }

            Gizmos.DrawLine(e1, e2);
            Gizmos.DrawLine(e2, e3);
            Gizmos.DrawLine(e3, e4);
            Gizmos.DrawLine(e4, e1);

            Gizmos.DrawLine(s1, e1);
            Gizmos.DrawLine(s2, e2);
            Gizmos.DrawLine(s3, e3);
            Gizmos.DrawLine(s4, e4);
        }

        [Obsolete("Should use the legacy gizmo draw")]
        public static void DrawDirectionalLightGizmo(Light directionalLight)
        {
            var gizmoSize = 0.2f;
            DrawWireDisc(directionalLight.transform.rotation, directionalLight.transform.position, directionalLight.gameObject.transform.forward, gizmoSize);
            Gizmos.DrawLine(directionalLight.transform.position, directionalLight.transform.position + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.up * gizmoSize, directionalLight.transform.position + directionalLight.transform.up * gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.up * -gizmoSize, directionalLight.transform.position + directionalLight.transform.up * -gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.right * gizmoSize, directionalLight.transform.position + directionalLight.transform.right * gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.right * -gizmoSize, directionalLight.transform.position + directionalLight.transform.right * -gizmoSize + directionalLight.transform.forward);
        }
    }
}
