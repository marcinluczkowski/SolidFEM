﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LA = MathNet.Numerics.LinearAlgebra;
using CSparse;
using CSD = CSparse.Double;
using CSparse.Double.Factorization;
using CSparse.Storage;

using Rhino.Geometry;

namespace SolidFEM.Classes
{
    /// <summary>
    /// A class for all the matrix methods needed in the finite element analysis. 
    /// </summary>
    public static class FEM_Matrices
    {
        /// <summary>
        /// Construct global stiffness matrix by assembling element stiffness matrices.
        /// </summary>
        /// <returns> Global stiffness matrix. </returns>
        public static LA.Matrix<double> GlobalStiffnessCSparse(List<Element> elements, int numNode, Material material, ref FEMLogger logger)
        {
            // Initiate empty matrix
            //CSD.DenseMatrix m = new CSD.DenseMatrix(numNode * 3, numNode * 3);
            LA.Matrix<double> m = LA.Matrix<double>.Build.Dense(numNode * 3, numNode * 3);
            List<double> elementSums = new List<double>();

            // test the functionality: 
            int[] testElInds = new int[] { 0, 21, 84, 105, 168, 189, 252, 273 };
            List<LA.Matrix<double>> testElsMat = new List<LA.Matrix<double>>();
            List<Element> testEls = new List<Element>();
            int count = 0;
            // delete the above after finished test
            foreach (Element element in elements)
            {
                List<int> con = element.Connectivity; // get the connectivity of each element

                // iterate over the connectivity indices
                LA.Matrix<double> K_local = CalculateElementMatrices(element, material, ref logger).Item1;
                elementSums.Add(K_local.AsColumnMajorArray().Sum(x => Math.Abs(x))); // the sum of each element

                if (testElInds.Contains(count))
                {
                    testElsMat.Add(K_local);
                    testEls.Add(element);
                }

                // loop nodes of elements
                for (int i = 0; i < con.Count; i++)
                {
                    for (int j = 0; j < con.Count; j++)
                    {
                        // loop relevant local stiffness contribution
                        for (int dofRow = 0; dofRow < 3; dofRow++)
                        {
                            for (int dofCol = 0; dofCol < 3; dofCol++)
                            {
                                m[3 * con[i] + dofRow, 3 * con[j] + dofCol] += K_local[3 * i + dofRow, 3 * j + dofCol];
                            }
                        }
                    }
                }
                count++; // delete after testing
            }

            // small code to round the values of the elements
            /*
            double[] colMayArray = m.AsColumnMajorArray();
            for (int i = 0; i < colMayArray.Length; i++)
            {
                colMayArray[i] = Math.Round(colMayArray[i], 14);
            }
            var globalK = LA.Matrix<double>.Build.DenseOfColumnMajor(numNode * 3, numNode * 3, colMayArray);
            */
            var sum_Element = m.AsColumnMajorArray().Sum();
            return m;
        }

        /// <summary>
        /// Calculate element stifness matrix and element strain matrix.
        /// </summary>
        /// <returns> Element stiffness and strain matrix.</returns>
        public static Tuple<LA.Matrix<double>, List<LA.Matrix<double>>> CalculateElementMatrices(Element element, Material material, ref FEMLogger logger)
        {
            // summary: calculate local K and B matrix
            int roundrecisionBMatrix = 6;
            int rpb = roundrecisionBMatrix;
            // material
            LA.Matrix<double> C = material.GetMaterialConstant();

            // shapefunction
            // create local stiffness matrix
            int numElementNodes = element.Nodes.Count;
            LA.Matrix<double> K_local = LA.Matrix<double>.Build.Dense(3 * numElementNodes, 3 * numElementNodes);

            // create local deformation matrix
            List<LA.Matrix<double>> B_local = new List<LA.Matrix<double>>();

            // Global coordinates of the (corner) nodes of the actual element
            LA.Matrix<double> globalCoordinates = LA.Matrix<double>.Build.Dense(numElementNodes, 3);

            List<Point3d> localCoordinates = FEM_Utility.LocalCartesianCoordinates(element);
            

            for (int i = 0; i < numElementNodes; i++)
            {
                globalCoordinates[i, 0] = Math.Round(element.Nodes[i].Coordinate.X, rpb); // column of x coordinates
                globalCoordinates[i, 1] = Math.Round(element.Nodes[i].Coordinate.Y, rpb); // column of y coordinates
                globalCoordinates[i, 2] = Math.Round(element.Nodes[i].Coordinate.Z, rpb); // colum of z coordinates
            }

            // Different methods for Hex8 and Tet4. Tet4 doesn't need gauss integration because B and Jacobian are constant!
            if (element.Type == "Hex8")
            {
                //Numerical integration
                //LA.Matrix<double> gaussNodes = FEM.GetNaturalCoordinate((double)Math.Sqrt((double)1 / (double)3), 3);
                var gaussCoordinates = FEM_Utility.GetGaussPointMatrix(2, element.Type); // by defaul we have a 2x2x2 integration of Hex8 element
                for (int n = 0; n < gaussCoordinates.RowCount; n++)  // loop gauss nodes
                {
                    // Substitute the natural coordinates into the symbolic expression
                    var r = gaussCoordinates.Row(n)[0];
                    var s = gaussCoordinates.Row(n)[1];
                    var t = gaussCoordinates.Row(n)[2];

                    // Partial derivatives of the shape functions
                    //LA.Matrix<double> shapeFunctionsDerivatedNatural = FEM.DerivateWithNatrualCoordinates(r, s, t, 3);
                    var partialDerivatives = FEM_Utility.PartialDerivateShapeFunctions(r, s, t, element.Type);

                    // Calculate Jacobian matrix
                    LA.Matrix<double> jacobianMatrix = partialDerivatives.Multiply(globalCoordinates);

                    // round the jacobian
                    var jColMajArr = jacobianMatrix.AsColumnMajorArray();
                    for (int k = 0; k < jColMajArr.Length; k++)
                    {
                        jColMajArr[k] = Math.Round(jColMajArr[k], rpb);
                    }
                    jacobianMatrix = LA.Matrix<double>.Build.DenseOfColumnMajor(3, 3, jColMajArr);
                    // Calculate B - LA.Matrix
                    LA.Matrix<double> shapeFuncDerivatedCartesian = (jacobianMatrix.Inverse()).Multiply(partialDerivatives);

                    double jacobianDeterminant = jacobianMatrix.Determinant();
                    //if (jacobianDeterminant < 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Negativ jac det"); }
                    if (jacobianDeterminant < 0) { logger.AddWarning("Negativ jacobian determeninant"); }
                    int dimRowB = 6;

                    // establish the B-matrix
                    LA.Matrix<double> B_i = LA.Double.DenseMatrix.Build.Dense(dimRowB, 3 * numElementNodes);

                    for (int i = 0; i < numElementNodes; i++)
                    {

                        // with the shape functions derivated with respect to the cartesian coordinates the rotated and unrotated element vectors are not the same... This is the correct one according to the formulas
                        var B_i_sub = LA.Double.DenseMatrix.Build.DenseOfRowMajor(6, 3, new double[] {
                            shapeFuncDerivatedCartesian.Row(0)[i], 0, 0,
                            0, shapeFuncDerivatedCartesian.Row(1)[i], 0,
                            0, 0, shapeFuncDerivatedCartesian.Row(2)[i],
                            shapeFuncDerivatedCartesian.Row(1)[i], shapeFuncDerivatedCartesian.Row(0)[i], 0,
                            shapeFuncDerivatedCartesian.Row(2)[i], 0, shapeFuncDerivatedCartesian.Row(0)[i],
                            0, shapeFuncDerivatedCartesian.Row(2)[i], shapeFuncDerivatedCartesian.Row(1)[i]
                            });

                        B_i.SetSubMatrix(0, i * 3, B_i_sub);

                    }

                    B_local.Add(B_i);
                    //K_local += ((B_i.Transpose()).Multiply(C).Multiply(B_i)).Multiply(jacobianDeterminant);
                    var k_i = (B_i.Transpose()).Multiply(C.Multiply(B_i)).Multiply(jacobianDeterminant);

                    K_local.Add(k_i, K_local);
                }
            }
            else if (element.Type == "Tet4")
            {
                var partialDerivatives = FEM_Utility.PartialDerivateShapeFunctions(1, 1, 1, "Tet4");    //Random coordinates (1,1,1) because the method requires coordinate inputs

                // Calculate Jacobian matrix
                LA.Matrix<double> jacobianMatrix = partialDerivatives.Multiply(globalCoordinates);

                // round the jacobian
                var jColMajArr = jacobianMatrix.AsColumnMajorArray();
                for (int k = 0; k < jColMajArr.Length; k++)
                {
                    jColMajArr[k] = Math.Round(jColMajArr[k], rpb);
                }
                jacobianMatrix = LA.Matrix<double>.Build.DenseOfColumnMajor(3, 3, jColMajArr);

                // Calculate B - LA.Matrix
                LA.Matrix<double> shapeFuncDerivatedCartesian = (jacobianMatrix.Inverse()).Multiply(partialDerivatives);

                int dimRowB = 6;
                LA.Matrix<double> B_i = LA.Double.DenseMatrix.Build.Dense(dimRowB, 3 * numElementNodes);

                for (int i = 0; i < numElementNodes; i++)
                {

                    // with the shape functions derivated with respect to the cartesian coordinates the rotated and unrotated element vectors are not the same... This is the correct one according to the formulas
                    var B_i_sub = LA.Double.DenseMatrix.Build.DenseOfRowMajor(6, 3, new double[] {
                            shapeFuncDerivatedCartesian.Row(0)[i], 0, 0,
                            0, shapeFuncDerivatedCartesian.Row(1)[i], 0,
                            0, 0, shapeFuncDerivatedCartesian.Row(2)[i],
                            shapeFuncDerivatedCartesian.Row(1)[i], shapeFuncDerivatedCartesian.Row(0)[i], 0,
                            shapeFuncDerivatedCartesian.Row(2)[i], 0, shapeFuncDerivatedCartesian.Row(0)[i],
                            0, shapeFuncDerivatedCartesian.Row(2)[i], shapeFuncDerivatedCartesian.Row(1)[i]
                            });


                    B_i.SetSubMatrix(0, i * 3, B_i_sub);

                }
                B_local.Add(B_i);
                // Get volume of Tetrahedra
                Brep triangle1 = Brep.CreateFromCornerPoints(element.TopologyVertices[0], element.TopologyVertices[1], element.TopologyVertices[2], 0.0001);
                Brep triangle2 = Brep.CreateFromCornerPoints(element.TopologyVertices[0], element.TopologyVertices[1], element.TopologyVertices[3], 0.0001);
                Brep triangle3 = Brep.CreateFromCornerPoints(element.TopologyVertices[0], element.TopologyVertices[2], element.TopologyVertices[3], 0.0001);
                Brep triangle4 = Brep.CreateFromCornerPoints(element.TopologyVertices[1], element.TopologyVertices[2], element.TopologyVertices[3], 0.0001);

                List<Brep> triangles = new List<Brep> { triangle1, triangle2, triangle3, triangle4 };

                Brep[] tetra = Brep.CreateSolid(triangles, 0.0001);

                VolumeMassProperties vmp = VolumeMassProperties.Compute(tetra[0]);
                double V = vmp.Volume;

                var k_i = V * (B_i.Transpose()).Multiply(C.Multiply(B_i));

                K_local.Add(k_i, K_local);
            }
            return Tuple.Create(K_local, B_local);
        }

        public static LA.Matrix<double> CalculateGlobalStiffnessMatrix(List<Element> elements, int numNode, Material material, ref FEMLogger logger)
        {

            // create stiffness matrix
            LA.Matrix<double> K_global = LA.Matrix<double>.Build.Dense(numNode * 3, numNode * 3);
            foreach (Element element in elements)
            {

                List<int> con = element.Connectivity;


                LA.Matrix<double> K_local = FEM_Matrices.CalculateElementMatrices(element, material, ref logger).Item1;

                // loop nodes of elements
                for (int i = 0; i < con.Count; i++)
                {
                    for (int j = 0; j < con.Count; j++)
                    {
                        // loop relevant local stiffness contribution
                        for (int dofRow = 0; dofRow < 3; dofRow++)
                        {
                            for (int dofCol = 0; dofCol < 3; dofCol++)
                            {
                                K_global[3 * con[i] + dofRow, 3 * con[j] + dofCol] += +K_local[3 * i + dofRow, 3 * j + dofCol];
                            }
                        }
                    }
                }

            }
            return K_global;
        }

        /// <summary>
        /// Reduce stiffness matrix and load vector for fixed boundary conditions.
        /// </summary>
        /// <returns> Reduced stiffness matrix and load vector. </returns>
        public static Tuple<LA.Matrix<double>, LA.Matrix<double>> ReduceKandR(LA.Matrix<double> K_global, LA.Matrix<double> R, List<int> BC)
        {
            int removeIndex = 0;
            for (int i = 0; i < K_global.RowCount; i++)
            {
                if (BC[i] == 1)
                {
                    K_global = K_global.RemoveRow(removeIndex);
                    K_global = K_global.RemoveColumn(removeIndex);
                    R = R.RemoveRow(removeIndex);
                    removeIndex--;
                }
                removeIndex++;
            }
            return Tuple.Create(K_global, R);
        }

        public static void ReduceMatrices(List<int> BC, int rowCount, ref LA.Matrix<double> K, ref LA.Matrix<double> R)
        {
            int subtract = 0;
            for (int i = 0; i < BC.Count; i++)
            {
                if (BC[i] == 1)
                {
                    K = K.RemoveColumn(i - subtract);
                    K = K.RemoveRow(i - subtract);

                    R = R.RemoveRow(i - subtract);
                    subtract--;
                }
            }


        }

    }
}
