﻿using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using PGK.Models;
using PGK.Views;
using System;
using System.IO;
using PGK.Services;

namespace PGK.Data
{
    [Table("Nodes")]
    public class NodeDatabase
    {
        static NodeDatabase dbNodes;
        public static NodeDatabase DBnodes
        {
            get
            {
                if (dbNodes == null)
                {
                    try
                    {
                        dbNodes = new
                        NodeDatabase(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Nodes.db3"));

                    }
                    catch (Exception ex)
                    {
                        DebugPage.AppendLine("NodeDatabase.DBnodes Error: " + ex.Message);
                    }
                }
                return dbNodes;
            }
        }
        
        readonly SQLiteAsyncConnection NodeConn;
        public NodeDatabase(string dbPath)
        {
            try
            {
                NodeConn = new SQLiteAsyncConnection(dbPath);
                NodeConn.CreateTableAsync<Node>().Wait();

            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("NodeDatabase() Error: " + ex.Message);
            }
        }
        public Task<List<Node>> GetBranchesAsync(string trunk)
        {
            Task<List<Node>> nodes = null;
            try
            {
                nodes = NodeConn.QueryAsync<Node>("SELECT * FROM [Node] WHERE LeafTag LIKE ?", trunk + "%");
            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("NodeDatabase.GetBranchesAsync Error: " + ex.Message);
            }
            return nodes;
        }
        public Task<List<Node>> SearchKeywordAsync(string keyword)
        {
            Task < List < Node >> nodes = null;
            try
            {
                nodes = NodeConn.QueryAsync<Node>("SELECT * FROM [Node] WHERE LeafTag LIKE ? OR Header LIKE ?", "%" + MarkerCodes.leafSeparator + keyword + "%", "%" + keyword + "%");
            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("NodeDatabase.SearchKeywordAsync Error: " + ex.Message);
            }
            return nodes;
        }
        public Task<Node> GetChildAsync(string LeafTag)
        {
            try
            {
                return NodeConn.Table<Node>().Where(i => i.LeafTag == LeafTag).FirstOrDefaultAsync();
            } catch(Exception ex)
            {
                DebugPage.AppendLine("NodeDatabase.GetChildAsync Error: " + ex.Message);
                return null;
            }
        }
        public async Task<int> UpdateNodeAsync(Node node)
        {
            int numberUpdated;
            try
            {
                numberUpdated = await NodeConn.UpdateAsync(node);
            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("NodeDatabase.UpdateNodeAsync Error: " + ex.Message);
                return -1;
            }

            return numberUpdated;
        }
        public async Task<int> InsertNodeAsync(Node node)
        {
            int numberInserted;
            try
            {
                numberInserted = await NodeConn.InsertAsync(node);
            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("NodeDatabase.InsertNodeAsync Error: " + ex.Message);
                return -1;
            }

            return numberInserted;
        }
        public async Task<int> SaveNodeSAsync(List<Node> nodes)
        {
            // Save, update or delete nodes.
            int numberProessedNodes = 0;
            int number;
            try
            {
                foreach (Node child in nodes)
                {
                    number = await SaveNodeAsync(child);
                    if (number < 0) return -1;
                    numberProessedNodes += number;
                }

            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("NodeDatabase.SaveNodeSAsync Error: " + ex.Message);
                return -1;
            }

            // Return the number of added, deleted or updated Nodes
            return numberProessedNodes;
        }
        public async Task<int> SaveNodeAsync(Node node)
        {

            // Save nodes.
            int numberProessedNodes = 0;
            int number;
            try
            {
                Node twin = await GetChildAsync(node.LeafTag);
                if (twin == null)
                {
                    number = await InsertNodeAsync(node);
                    if (number < 0) return -1;
                    numberProessedNodes += number;
                }
                else
                {
                    if (node.IsDeleted)
                    {
                        number = await DeleteNodeAsync(node);
                        if (number < 0) return -1;
                        numberProessedNodes += number;
                    }
                    else
                    {
                        number = await UpdateNodeAsync(node);
                        if (number < 0) return -1;
                        numberProessedNodes += number;
                    }

                }

            }
            catch (Exception ex)
            {
                DebugPage.AppendLine("NodeDatabase.SaveNodeAsync Error: " + ex.Message);
                 return -1;
            }

            // Return the number of added or updated Nodes
            return numberProessedNodes;
        }
        public async Task<int> DeleteNodeAsync(Node node)
        {
            int numberProessedNodes = 0;
            try
            {
                numberProessedNodes += await NodeConn.DeleteAsync(node);
            }
            catch (Exception err)
            {
                DebugPage.AppendLine("SaveNodeAsync: " + err.Message);
                numberProessedNodes = -1;
            }

            return numberProessedNodes;
        }
    }
}