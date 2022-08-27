using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace MSS.Types
{
	public abstract class TreeNode<U, V> : ITreeNode<U,V>, INotifyPropertyChanged, ICloneable where U : class, ITreeNode<U,V> where V : IEquatable<V>, IComparable<V>
	{
		private bool _isCurrent;
		private bool _isExpanded;

		#region Constructor

		public TreeNode(V item, U? parentNode)
			: this(item, parentNode, isRoot: false, isHome: false, isCurrent: false, isExpanded: false)
		{ }

		protected TreeNode(V item, U? parentNode, bool isRoot, bool isHome, bool isCurrent, bool isExpanded)
		{
			Item = item ?? throw new ArgumentNullException(nameof(item));
			ParentNode = parentNode;
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
		public U? ParentNode { get; set; }
		public abstract ObservableCollection<U> Children { get; init; }
		public bool IsDirty { get; set; }

		#endregion

		#region Branch Properties

		public bool IsRoot { get; init; }
		public bool IsHome { get; protected set; }
		public bool IsOrphan => ParentNode is null;

		#endregion

		#region UI Properties

		public virtual bool IsCurrent
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

		public abstract void AddNode(U node);

		public bool Move(U destination)
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
			var result = parentNode.Remove((U)(ITreeNode<U, V>)this);
			destination.AddNode((U)(ITreeNode<U, V>)this);

			return result;
		}

		public virtual bool Remove(U node)
		{
			node.ParentNode = null;
			var result = Children.Remove(node);

			return result;
		}

		public int GetSortPosition(U node)
		{
			var cnt = Children.Count;
			if (cnt == 0)
			{
				return 0;
			}

			var item = node.Item;

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

		public IList<U> GetAncestors()
		{
			var result = new List<U>();

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

			_ = IsHome ? sb.Append(" [Home]") : IsRoot ? sb.Append(" [Root]") : sb.Append($" ParentId: {ParentNode?.Id}");

			_ = sb.Append($" Children: {Children.Count};");

			return sb.ToString();
		}

		public abstract object Clone();

		#endregion
	}
}
