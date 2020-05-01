using FlowTask_Backend;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;

namespace FlowTask_Test
{
    [TestClass]
    public class GraphUnitTest
    {
        [TestMethod]
        public void TestGraph()
        {
            Random randy = new Random();
            for (int i = 0; i < 1000; i++)
            {
                Graph test_graph = new Graph();

                int nodeCount = randy.Next(0, 250);

                for(int j = 0; j < nodeCount; j ++)
                {
                    Node n = new Node("test", 1, false, DateTime.Now, "", 0, j);
                    test_graph.AddNode(n);
                }

                for (int j = 0; j < nodeCount; j++)
                    for (int k = j + 1; k < nodeCount; k++)
                        if(randy.Next(0,2) == 1)
                            test_graph.CreateEdge(test_graph.Nodes[j], test_graph.Nodes[k]);

                string db_format = test_graph.GetDBFormatAdjacency();

                Graph make_from_string = new Graph(0, test_graph.Nodes, db_format);

                Assert.AreEqual(make_from_string.GetDBFormatAdjacency(), db_format);
            }
        }
            
        [TestMethod]
        public void TestEmptyGraph()
        {
            Graph test_graph = new Graph(0, new List<Node>(), "");
            Assert.IsNotNull(test_graph);
            Assert.IsNotNull(test_graph.GetDBFormatAdjacency());
        }


        [TestMethod]
        public void TestGetNeighbors()
        {
            Node n1 = new Node(0, "n1", 0, false, DateTime.Now, "", 0, 0);
            Node n2 = new Node(1, "n2", 0, false, DateTime.Now, "", 0, 1);
            Node n3 = new Node(2, "n3", 0, false, DateTime.Now, "", 0, 2);
            Node n4 = new Node(3, "n4", 0, false, DateTime.Now, "", 0, 3);
            Node n5 = new Node(4, "n5", 0, false, DateTime.Now, "", 0, 4);
            Node n6 = new Node(5, "n6", 0, false, DateTime.Now, "", 0, 5);

            List<Node> nodes = new List<Node>(new Node[] { n1, n2, n3, n4, n5, n6});
            Graph test_graph = new Graph(0, nodes, "");

            test_graph.CreateEdge(n2, n3);
            test_graph.CreateEdge(n3, n4);
            test_graph.CreateEdge(n3, n5);
            test_graph.CreateEdge(n3, n6);

            List<Node> n1_neighbors = test_graph.GetNeighbors(n1.NodeID);
            List<Node> n2_neighbors = test_graph.GetNeighbors(n2.NodeID);
            List<Node> n3_neighbors = test_graph.GetNeighbors(n3.NodeID);
            
            Assert.IsNotNull(n1_neighbors);
            Assert.IsTrue(n1_neighbors.Count == 0);
            Assert.IsNotNull(n2_neighbors);
            Assert.IsTrue(n2_neighbors.Count == 1);
            Assert.IsTrue(n2_neighbors.Contains(n3));
            Assert.IsNotNull(n3_neighbors);
            Assert.IsTrue(n3_neighbors.Count == 3);
            Assert.IsTrue(n3_neighbors.Contains(n4));
            Assert.IsTrue(n3_neighbors.Contains(n5));
            Assert.IsTrue(n3_neighbors.Contains(n6));
        }
        
        [TestMethod]
        public void TestSoonestDate()
        {
            Node n1 = new Node(0, "n1", 0, false, DateTime.Now, "", 0, 0);
            Node n2 = new Node(1, "n2", 0, false, DateTime.Now.AddDays(1), "", 0, 1);
            Node n3 = new Node(2, "n3", 0, false, DateTime.Now.AddDays(2), "", 0, 2);
            Node n4 = new Node(3, "n4", 0, false, DateTime.Now.AddDays(3), "", 0, 3);
            Node n5 = new Node(4, "n5", 0, false, DateTime.Now.AddDays(4), "", 0, 4);
            Node n6 = new Node(5, "n6", 0, false, DateTime.Now.AddDays(5), "", 0, 5);

            List<Node> nodes = new List<Node>(new Node[] { n1, n2, n3, n4, n5, n6 });
            Graph test_graph = new Graph(0, nodes, "");

            DateTime soonest = test_graph.GetSoonestDate();
            Assert.AreEqual(n1.Date, soonest);

            n1.SetCompleteStatus(true);
            soonest = test_graph.GetSoonestDate();
            Assert.AreEqual(n2.Date, soonest);

            foreach (Node n in nodes)
                n.SetCompleteStatus(true);
            soonest = test_graph.GetSoonestDate();
            Assert.AreEqual(DateTime.Now.ToString("dddd, dd MMMM yyyy"), soonest.ToString("dddd, dd MMMM yyyy"));

            n3.SetCompleteStatus(false);
            n5.SetCompleteStatus(false);
            soonest = test_graph.GetSoonestDate();
            Assert.AreEqual(n3.Date, soonest);
        }

        [TestMethod]
        public void TestSoonestNode()
        {
            Node n1 = new Node(0, "n1", 0, false, DateTime.Now, "", 0, 0);
            Node n2 = new Node(1, "n2", 0, false, DateTime.Now.AddDays(1), "", 0, 1);
            Node n3 = new Node(2, "n3", 0, false, DateTime.Now.AddDays(2), "", 0, 2);
            Node n4 = new Node(3, "n4", 0, false, DateTime.Now.AddDays(3), "", 0, 3);
            Node n5 = new Node(4, "n5", 0, false, DateTime.Now.AddDays(4), "", 0, 4);
            Node n6 = new Node(5, "n6", 0, false, DateTime.Now.AddDays(5), "", 0, 5);

            List<Node> nodes = new List<Node>(new Node[] { n1, n2, n3, n4, n5, n6 });
            Graph test_graph = new Graph(0, nodes, "");

            Node soonest = test_graph.GetSoonestNode();
            Assert.AreEqual(n1, soonest);

            n1.SetCompleteStatus(true);
            soonest = test_graph.GetSoonestNode();
            Assert.AreEqual(n2, soonest);

            foreach (Node n in nodes)
                n.SetCompleteStatus(true);
            soonest = test_graph.GetSoonestNode();
            Assert.IsNull(soonest);

            n3.SetCompleteStatus(false);
            n5.SetCompleteStatus(false);
            soonest = test_graph.GetSoonestNode();
            Assert.AreEqual(n3, soonest);
        }


    }
}
