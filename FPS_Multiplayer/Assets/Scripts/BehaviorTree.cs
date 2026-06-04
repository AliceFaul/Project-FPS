using UnityEngine;
using System.Collections.Generic;

public enum NodeStatus { // The status of a node in the behavior tree
    Success,
    Failure,
    Running
}

public abstract class Node { 
    protected NodeStatus status; // The current status of the node
    public NodeStatus Status { get { return status; } } // Property to access the status of the node
    public abstract NodeStatus Evaluate(); // The method to evaluate the node
}

// A selector node will evaluate its children until one of them returns success.

// If a child returns success, the selector returns success.
// If all children return failure, the selector returns failure.
// If any child returns running, the selector returns running.

public class Selector : Node {
    protected List<Node> nodes = new List<Node>(); // The list of child nodes

    protected Selector(List<Node> nodes) { 
        this.nodes = nodes; // Constructor to initialize the list of child nodes
    }

    public override NodeStatus Evaluate() { 
        foreach(Node node in nodes) { // Loop through each child node
            switch(node.Evaluate()) { // Evaluate the child node and check its status
                case NodeStatus.Success:
                    status = NodeStatus.Success; // If the child returns success, set the selector's status to success
                    return status; // Return the selector's status
                case NodeStatus.Failure:
                    continue; // If the child returns failure, continue to the next child
                case NodeStatus.Running:
                    status = NodeStatus.Running; // If the child returns running, set the selector's status to running
                    return status; // Return the selector's status
            }
        }
        status = NodeStatus.Failure; // If all children return failure, set the selector's status to failure
        return status; // Return the selector's status
    }
}

// A sequence node will evaluate its children until one of them returns failure.
public class Sequence : Node {
    protected List<Node> nodes = new List<Node>(); // The list of child nodes

    public Sequence(List<Node> nodes) { 
        this.nodes = nodes; // Constructor to initialize the list of child nodes
    }

    public override NodeStatus Evaluate() { 
        bool anyChildRunning = false; // Flag to check if any child is running
        foreach(Node node in nodes) { // Loop through each child node
            switch(node.Evaluate()) { // Evaluate the child node and check its status
                case NodeStatus.Failure:
                    status = NodeStatus.Failure; // If the child returns failure, set the sequence's status to failure
                    return status; // Return the sequence's status
                case NodeStatus.Success:
                    continue; // If the child returns success, continue to the next child
                case NodeStatus.Running:
                    anyChildRunning = true; // If the child returns running, set the flag to true
                    continue; // Continue to the next child
            }
        }
        status = anyChildRunning ? NodeStatus.Running : NodeStatus.Success; // If any child is running, set the sequence's status to running, otherwise set it to success
        return status; // Return the sequence's status
    }
}

// A task node will execute a specific task and return its status.
public class TaskNode : Node { 
    public delegate NodeStatus TaskDelegate(); // Delegate to define the task function
    private TaskDelegate task; // The task function to execute

    public TaskNode(TaskDelegate task) { 
        this.task = task; // Constructor to initialize the task function
    }

    public override NodeStatus Evaluate() { 
        status = task(); // Execute the task function and set the node's status to the result
        return status; // Return the node's status
    }
}

//public class BehaviorTree : MonoBehaviour {
    
//}
