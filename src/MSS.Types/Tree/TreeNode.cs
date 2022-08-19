using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace MSS.Types
{
	public abstract class TreeNode<U, V> : ITreeNode<U,V>, INotifyPropertyChanged, ICloneable where U : ITreeItem<V> where V : IEquatable<V>, IComparable<V>
	{
		private bool _isCurrent;
		private bool _isExpanded;

		#region Constructor

		public TreeNode(V item, ITreeNode<U, V>? parentNode)
			: this(item, parentNode, isRoot: false, isHome: false, isCurrent: false, isExpanded: false)
		{ }

		protected TreeNode(V item, ITreeNode<U, V>? parentNode, bool isRoot, bool isHome, bool isCurrent, bool isExpanded)
		{
			Item = item ?? throw new ArgumentNullException(nameof(item));
			ParentNode = parentNode;
			Children = new ObservableCollection<ITreeNode<U, V>>();
			IsDirty = false;
			IsRoot = isRoot;
			IsHome = isHome;
			_isCurrent = isCurrent;
			_isExpanded = isExpanded;
		}

		#endregion

		#region Public Properties

		public V Item { get; init; }
		public ObjectId Id { get; protected set; }
		public ObjectId? ParentId { get; protected set; }
		public ITreeNode<U,V>? ParentNode { get; set; }
		public U Node => (U)(ITreeNode<U, V>)this;
		public abstract ObservableCollection<ITreeNode<U,V>> Children { get; init; }
		public bool IsDirty { get; set; }

		#endregion

		#region Branch Properties

		public bool IsRoot { get; init; }
		public bool IsHome { get; protected set; }
		public bool IsOrphan => ParentNode is null;

		#endregion

		#region UI Properties

		public bool IsCurrent
		{
			get => _isCurrent;
			set
			{
				if (value != _isCurrent)
				{
					_isCurrent = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsExpanded
		{
			get => _isExpanded;
			set
			{
				if (value != _isExpanded)
				{
					_isExpanded = value;
					OnPropertyChanged();
				}
			}
		}

		public string IdAndParentId
		{
			get
			{
				var result = Id + ", " + (ParentId.ToString() ?? "null");
				return result;
			}
		}

		#endregion

		#region Public Methods 

		public abstract U AddItem(V job);

		public virtual void AddNode(ITreeNode<U, V> node)
		{
			if (!Children.Any())
			{
				Children.Add(node);
			}
			else
			{
				var index = GetSortPosition(node.Item);
				if (index < 0)
				{
					index = ~index;
				}

				Children.Insert(index, node);
			}

			node.ParentNode = this;
		}

		public bool Move(ITreeNode<U, V> destination)
		{
			if (IsRoot)
			{
				throw new InvalidOperationException("Moving the root node is not supported.");
			}

			if (ParentNode is null)
			{
				throw new InvalidOperationException("Cannot move an orphan JobTreeItem.");
			}

			var parentNode = ParentNode;
			var result = parentNode.Remove(this);
			destination.AddNode(this);

			return result;
		}

		public virtual bool Remove(ITreeNode<U, V> jobTreeItem)
		{
			jobTreeItem.ParentNode = null;
			bool result = Children.Remove(jobTreeItem);

			return result;
		}

		public int GetSortPosition(V item)
		{
			var cnt = Children.Count;
			if (cnt == 0)
			{
				return 0;
			}

			// If the item is greater than the last item, return an index indicating "add to end."
			if (Children[^1].Item.CompareTo(item) < 0)
			{
				return cnt;
			}

			// If the item is smaller than the first item, return 0.
			if (Children[0].Item.CompareTo(item) > 0)
			{
				return 0;
			}

			for (var i = 0; i < cnt; i++)
			{
				if (Children[i].Item.Equals(item))
				{
					return i;
				}

				if (Children[i].Item.CompareTo(item) > 0)
				{
					return ~i;
				}
			}

			return cnt;
		}

		public List<ITreeNode<U, V>> GetAncestors()
		{
			var result = new List<ITreeNode<U, V>>
			{
				this
			};

			var parentNode = ParentNode;

			while (parentNode != null)
			{
				result.Add(parentNode);
				parentNode = parentNode.ParentNode;
			}

			return result;
		}

		#endregion

		#region Property Changed Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		#region ToString and ICloneable Support

		public override string ToString()
		{
			var sb = new StringBuilder()
				.Append($" Id: {Id}");

			if (IsHome)
			{
				_ = sb.Append(" [Home]");
			}
			else if (IsRoot)
			{
				_ = sb.Append(" [Root]");
			}
			else
			{
				_ = sb.Append($" ParentId: {ParentNode?.Id}");
			}

			_ = sb.Append($" Children: {Children.Count};");

			return sb.ToString();
		}

		public abstract object Clone();

		#endregion
	}
}
