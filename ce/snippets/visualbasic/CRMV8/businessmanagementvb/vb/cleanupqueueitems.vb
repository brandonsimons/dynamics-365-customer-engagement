' <snippetcleanupqueueitems>


Imports System.ServiceModel
Imports System.ServiceModel.Description

' These namespaces are found in the Microsoft.Xrm.Sdk.dll assembly
' found in the SDK\bin folder.
Imports Microsoft.Xrm.Sdk
Imports Microsoft.Xrm.Sdk.Query
Imports Microsoft.Xrm.Sdk.Discovery
Imports Microsoft.Xrm.Sdk.Messages

' This namespace is found in Microsoft.Crm.Sdk.Proxy.dll assembly
' found in the SDK\bin folder.
Imports Microsoft.Crm.Sdk.Messages
Imports Microsoft.Xrm.Sdk.Client

Namespace Microsoft.Crm.Sdk.Samples
    ''' <summary>
    ''' This Sample shows how to delete inactive items in a queue.
    ''' </summary>
    Public Class CleanUpQueueItems
        #Region "Class Level Members"

        ' Define the IDs needed for this sample.
        Private _queueId As Guid
        Private _phoneCallId As Guid
        Private _serviceProxy As OrganizationServiceProxy

        #End Region ' Class Level Members

        #Region "How To Sample Code"
        ''' <summary>
        ''' Create and configure the organization service proxy.
        ''' Initiate the method to create any data that this sample requires.
        ''' Retrieve all queueitems with inactive phone calls from a queue.
        ''' Delete all inactive phone call entity instances.
        ''' Optionally delete any entity records that were created for this sample.
        ''' <param name="serverConfig">Contains server connection information.</param>
        ''' <param name="promptforDelete">When True, the user will be prompted to delete all
        ''' created entities.</param>
        ''' </summary>
        Public Sub Run(ByVal serverConfig As ServerConnection.Configuration, ByVal promptForDelete As Boolean)
            Try

                ' Connect to the Organization service. 
                ' The using statement assures that the service proxy will be properly disposed.
                _serviceProxy = ServerConnection.GetOrganizationProxy(serverConfig)
                Using _serviceProxy
                    ' This statement is required to enable early-bound type support.
                    _serviceProxy.EnableProxyTypes()

                    CreateRequiredRecords()


                    ' Retrieve all queueitems with inactive phone calls from a queue.
                    Dim retrieveItemsForDeletion As QueryExpression = New QueryExpression With { _
                        .EntityName = QueueItem.EntityLogicalName, .ColumnSet = New ColumnSet("queueitemid")}
                    retrieveItemsForDeletion.Criteria = New FilterExpression()
                    retrieveItemsForDeletion.Criteria.AddCondition("queueid", ConditionOperator.Equal, {_queueId})
                    retrieveItemsForDeletion.Criteria.FilterOperator = LogicalOperator.And
                    retrieveItemsForDeletion.AddLink(PhoneCall.EntityLogicalName, "objectid", "activityid", JoinOperator.Inner).LinkCriteria() = _
                        New FilterExpression()
                    retrieveItemsForDeletion.LinkEntities(0).LinkCriteria().AddCondition("statecode", ConditionOperator.NotEqual, _
                                                                                         {CInt(Fix(PhoneCallState.Open))})
                        ' Only include queueitems for this queue.
                        ' Join to the related phonecall entities.
                            ' Only include phone calls if their state is not Open.

                    Dim itemCollection As EntityCollection = _serviceProxy.RetrieveMultiple(retrieveItemsForDeletion)

                    ' Loop through the results and delete each queueitem.
                    For Each entity As Entity In itemCollection.Entities
                        Dim item As QueueItem = CType(entity, QueueItem)
                        _serviceProxy.Delete(QueueItem.EntityLogicalName, item.QueueItemId.Value)
                    Next entity

                    Console.WriteLine("Inactive phonecalls have been deleted from the queue.")

                    DeleteRequiredRecords(promptForDelete)

                End Using
            ' Catch any service fault exceptions that Microsoft Dynamics CRM throws.
            Catch fe As FaultException(Of Microsoft.Xrm.Sdk.OrganizationServiceFault)
                ' You can handle an exception here or pass it back to the calling method.
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' This method creates any entity records that this sample requires.
        ''' Create a queue instance. 
        ''' Create a phone call activity instance.
        ''' Add phone call entity instance in to queue.
        ''' Mark phone call entity instance status as completed.
        ''' </summary>
        Public Sub CreateRequiredRecords()
            ' Create a queue instance and set its property values.
            Dim newQueue As New Queue() With {.Name = "Example Queue", .Description = "This is an example queue."}

            _queueId = _serviceProxy.Create(newQueue)
            Console.WriteLine("Created {0}.", newQueue.Name)

            ' Create a phone call activity instance.
            Dim newPhoneCall As PhoneCall = New PhoneCall With {.Description = "Example Phone Call"}

            _phoneCallId = _serviceProxy.Create(newPhoneCall)
            Console.WriteLine("Created {0}.", newPhoneCall.Description)

            ' Use AddToQueue message to add an entity into a queue, which will associate
            ' the phone call activity with the queue.
            Dim addToSourceQueue As AddToQueueRequest = New AddToQueueRequest With { _
                .DestinationQueueId = _queueId, .Target = New EntityReference(PhoneCall.EntityLogicalName, _phoneCallId)}

            _serviceProxy.Execute(addToSourceQueue)
            Console.WriteLine("Added phone call entity instance to {0}", newQueue.Name)

            ' Mark the phone call as completed.
            Dim setStatePhoneCall As SetStateRequest = New SetStateRequest With { _
                .EntityMoniker = New EntityReference(PhoneCall.EntityLogicalName, _phoneCallId), _
                .State = New OptionSetValue(CInt(Fix(PhoneCallState.Completed))), .Status = New OptionSetValue(-1)}

            _serviceProxy.Execute(setStatePhoneCall)
            Console.WriteLine("PhoneCall entity instance has been marked as completed.")

            Return
        End Sub

        ''' <summary>
        ''' Deletes any entity records that were created for this sample.
        ''' <param name="prompt">Indicates whether to prompt the user 
        ''' to delete the records created in this sample.</param>
        ''' </summary>
        Public Sub DeleteRequiredRecords(ByVal prompt As Boolean)
            Dim deleteRecords As Boolean = True

            If prompt Then
                Console.WriteLine(vbLf &amp; "Do you want these entity records deleted? (y/n)")
                Dim answer As String = Console.ReadLine()

                deleteRecords = (answer.StartsWith("y") OrElse answer.StartsWith("Y"))
            End If

            If deleteRecords Then
                _serviceProxy.Delete(PhoneCall.EntityLogicalName, _phoneCallId)
                _serviceProxy.Delete(Queue.EntityLogicalName, _queueId)

                Console.WriteLine("Entity records have been deleted.")
            End If
        End Sub

        #End Region ' How To Sample Code

        #Region "Main"
        ''' <summary>
        ''' Main. Runs the sample and provides error output.
        ''' <param name="args">Array of arguments to Main method.</param>
        ''' </summary>
        Public Shared Sub Main(ByVal args() As String)
            Try
                ' Obtain the target organization's Web address and client logon 
                ' credentials from the user.
                Dim serverConnect As New ServerConnection()
                Dim config As ServerConnection.Configuration = serverConnect.GetServerConfiguration()

                Dim app As New CleanUpQueueItems()
                app.Run(config, True)

            Catch ex As FaultException(Of Microsoft.Xrm.Sdk.OrganizationServiceFault)
                Console.WriteLine("The application terminated with an error.")
                Console.WriteLine("Timestamp: {0}", ex.Detail.Timestamp)
                Console.WriteLine("Code: {0}", ex.Detail.ErrorCode)
                Console.WriteLine("Message: {0}", ex.Detail.Message)
                Console.WriteLine("Plugin Trace: {0}", ex.Detail.TraceText)
                Console.WriteLine("Inner Fault: {0}", If(Nothing Is ex.Detail.InnerFault, "No Inner Fault", "Has Inner Fault"))
            Catch ex As TimeoutException
                Console.WriteLine("The application terminated with an error.")
                Console.WriteLine("Message: {0}", ex.Message)
                Console.WriteLine("Stack Trace: {0}", ex.StackTrace)
                Console.WriteLine("Inner Fault: {0}", If(Nothing Is ex.InnerException.Message, "No Inner Fault", ex.InnerException.Message))
            Catch ex As Exception
                Console.WriteLine("The application terminated with an error.")
                Console.WriteLine(ex.Message)

                ' Display the details of the inner exception.
                If ex.InnerException IsNot Nothing Then
                    Console.WriteLine(ex.InnerException.Message)

                    Dim fe As FaultException(Of Microsoft.Xrm.Sdk.OrganizationServiceFault) = _
                        TryCast(ex.InnerException, FaultException(Of Microsoft.Xrm.Sdk.OrganizationServiceFault))
                    If fe IsNot Nothing Then
                        Console.WriteLine("Timestamp: {0}", fe.Detail.Timestamp)
                        Console.WriteLine("Code: {0}", fe.Detail.ErrorCode)
                        Console.WriteLine("Message: {0}", fe.Detail.Message)
                        Console.WriteLine("Plugin Trace: {0}", fe.Detail.TraceText)
                        Console.WriteLine("Inner Fault: {0}", If(Nothing Is fe.Detail.InnerFault, "No Inner Fault", "Has Inner Fault"))
                    End If
                End If
            ' Additional exceptions to catch: SecurityTokenValidationException, ExpiredSecurityTokenException,
            ' SecurityAccessDeniedException, MessageSecurityException, and SecurityNegotiationException.


            Finally
                Console.WriteLine("Press <Enter> to exit.")
                Console.ReadLine()
            End Try

        End Sub
        #End Region ' Main
    End Class
End Namespace

' </snippetcleanupqueueitems>