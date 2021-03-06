﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRFSharp
{
    public class Tagger
    {
        public List<List<string>> x_;
        public Node[,] node_; //Node matrix
        public short ysize_;
        public short word_num; //the number of tokens need to be labeled
        public double Z_;  //概率值
        public double cost_;  //The path cost
        public short[] result_;
        public List<long[]> feature_cache_;

        //Calculate the cost of each path. It's used for finding the best or N-best result
        public int viterbi()
        {
            double bestc = double.MinValue;
            Node bestNode = null;

            for (int i = 0; i < word_num; ++i)
            {
                for (int j = 0; j < ysize_; ++j)
                {
                    bestc = double.MinValue;
                    bestNode = null;

                    Node node_i_j = node_[i, j];

                    foreach (CRFSharp.Path p in node_i_j.lpathList)
                    {
                        double cost = p.lnode.bestCost + p.cost + node_i_j.cost;
                        if (cost > bestc)
                        {
                            bestc = cost;
                            bestNode = p.lnode;
                        }
                    }

                    node_i_j.prev = bestNode;
                    node_i_j.bestCost = bestNode != null ? bestc : node_i_j.cost;
                }
            }

            bestc = double.MinValue;
            bestNode = null;

            short s = (short)(word_num - 1);
            for (short j = 0; j < ysize_; ++j)
            {
                if (bestc < node_[s, j].bestCost)
                {
                    bestNode = node_[s, j];
                    bestc = node_[s, j].bestCost;
                }
            }

            Node n = bestNode;
            while (n != null)
            {
                result_[n.x] = n.y;
                n = n.prev;
            }

            cost_ = -node_[s, result_[s]].bestCost;

            return Utils.ERROR_SUCCESS;
        }

        private void calcAlpha(int m, int n)
        {
            Node nd = node_[m, n];
            nd.alpha = 0.0;

            int i = 0;
            foreach (CRFSharp.Path p in nd.lpathList)
            {
                nd.alpha = Utils.logsumexp(nd.alpha, p.cost + p.lnode.alpha, (i == 0));
                i++;
            }
            nd.alpha += nd.cost;
        }

        private void calcBeta(int m, int n)
        {
            Node nd = node_[m, n];
            nd.beta = 0.0f;
            if (m + 1 < word_num)
            {
                int i = 0;
                foreach (CRFSharp.Path p in nd.rpathList)
                {
                    nd.beta = Utils.logsumexp(nd.beta, p.cost + p.rnode.beta, (i == 0));
                    i++;
                }
            }
            nd.beta += nd.cost;
        }

        public void forwardbackward()
        {
            for (int i = 0, k = word_num - 1; i < word_num; ++i, --k)
            {
                for (int j = 0; j < ysize_; ++j)
                {
                    calcAlpha(i, j);
                    calcBeta(k, j);
                }
            }

            Z_ = 0.0;
            for (int j = 0; j < ysize_; ++j)
            {
                Z_ = Utils.logsumexp(Z_, node_[0, j].beta, j == 0);
            }
        }


        //Assign feature ids to node and path
        public int RebuildFeatures()
        {
            int fid = 0;
            for (short cur = 0; cur < word_num; cur++)
            {
                for (short i = 0; i < ysize_; i++)
                {
                    node_[cur, i].fid = fid;
                }
                fid++;
            }

            for (int cur = 1; cur < word_num; cur++)
            {
                for (int j = 0; j < ysize_; ++j)
                {
                    foreach (CRFSharp.Path path in node_[cur - 1, j].rpathList)
                    {
                        path.fid = fid;
                    }
                }
                fid++;
            }

            return 0;
        }
    }
}
