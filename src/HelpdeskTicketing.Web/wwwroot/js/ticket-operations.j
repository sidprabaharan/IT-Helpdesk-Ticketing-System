/**
 * IT Helpdesk Ticketing System
 * JavaScript for ticket operations
 */

$(document).ready(function() {
    // Initialize rich text editor for ticket description and comments
    if (typeof tinymce !== 'undefined' && $('.rich-text-editor').length) {
        tinymce.init({
            selector: '.rich-text-editor',
            height: 300,
            menubar: false,
            plugins: [
                'advlist autolink lists link image charmap print preview anchor',
                'searchreplace visualblocks code fullscreen',
                'insertdatetime media table paste code help wordcount'
            ],
            toolbar: 'undo redo | formatselect | bold italic backcolor | alignleft aligncenter alignright alignjustify | bullist numlist outdent indent | removeformat | help',
        });
    }

    // Handle ticket assignment
    $('#assign-ticket-btn').click(function() {
        var ticketId = $(this).data('ticket-id');
        $('#assign-ticket-modal').modal('show');
        $('#ticket-id-for-assignment').val(ticketId);
    });

    // Handle 2-click resolution
    $('.quick-resolve-btn').click(function() {
        var ticketId = $(this).data('ticket-id');
        
        // Show quick resolution modal
        $('#quick-resolve-modal').modal('show');
        $('#ticket-id-for-resolution').val(ticketId);
    });

    // Submit quick resolution form
    $('#quick-resolve-form').submit(function(e) {
        e.preventDefault();
        
        var ticketId = $('#ticket-id-for-resolution').val();
        var resolutionNote = $('#resolution-note').val();
        
        $.ajax({
            url: '/api/tickets/' + ticketId + '/resolve',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                resolutionNote: resolutionNote
            }),
            success: function(result) {
                // Hide modal
                $('#quick-resolve-modal').modal('hide');
                
                // Show success message
                showNotification('Ticket resolved successfully!', 'success');
                
                // Refresh page after a short delay
                setTimeout(function() {
                    window.location.reload();
                }, 1500);
            },
            error: function(xhr, status, error) {
                // Show error message
                showNotification('Failed to resolve ticket: ' + xhr.responseText, 'error');
            }
        });
    });

    // Handle ticket status change
    $('.change-status-btn').click(function() {
        var ticketId = $(this).data('ticket-id');
        var currentStatus = $(this).data('current-status');
        
        // Populate and show status change modal
        $('#status-change-modal').modal('show');
        $('#ticket-id-for-status').val(ticketId);
        $('#current-status').text(currentStatus);
        $('#new-status').val('');
    });

    // Submit status change form
    $('#status-change-form').submit(function(e) {
        e.preventDefault();
        
        var ticketId = $('#ticket-id-for-status').val();
        var newStatus = $('#new-status').val();
        
        $.ajax({
            url: '/api/tickets/' + ticketId,
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({
                status: newStatus
            }),
            success: function(result) {
                // Hide modal
                $('#status-change-modal').modal('hide');
                
                // Show success message
                showNotification('Status updated successfully!', 'success');
                
                // Refresh page after a short delay
                setTimeout(function() {
                    window.location.reload();
                }, 1500);
            },
            error: function(xhr, status, error) {
                // Show error message
                showNotification('Failed to update status: ' + xhr.responseText, 'error');
            }
        });
    });

    // Add comment to ticket
    $('#add-comment-form').submit(function(e) {
        e.preventDefault();
        
        var ticketId = $('#ticket-id-for-comment').val();
        var commentContent = '';
        
        // If using TinyMCE, get content from editor
        if (typeof tinymce !== 'undefined') {
            commentContent = tinymce.get('comment-content').getContent();
        } else {
            commentContent = $('#comment-content').val();
        }
        
        var isInternal = $('#comment-is-internal').is(':checked');
        
        $.ajax({
            url: '/api/tickets/' + ticketId + '/comments',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                content: commentContent,
                isInternal: isInternal
            }),
            success: function(result) {
                // Clear comment form
                if (typeof tinymce !== 'undefined') {
                    tinymce.get('comment-content').setContent('');
                } else {
                    $('#comment-content').val('');
                }
                $('#comment-is-internal').prop('checked', false);
                
                // Show success message
                showNotification('Comment added successfully!', 'success');
                
                // Append the new comment to the comments list
                appendNewComment(result);
            },
            error: function(xhr, status, error) {
                // Show error message
                showNotification('Failed to add comment: ' + xhr.responseText, 'error');
            }
        });
    });

    // Append a new comment to the comments list without refreshing the page
    function appendNewComment(comment) {
        var commentHtml = `
            <div class="card mb-3 ${comment.isInternal ? 'border-warning' : ''}">
                <div class="card-header bg-light">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <strong>${comment.author}</strong>
                            ${comment.isInternal ? '<span class="badge badge-warning ml-2">Internal</span>' : ''}
                        </div>
                        <small class="text-muted">${formatDateTime(new Date(comment.createdAt))}</small>
                    </div>
                </div>
                <div class="card-body">
                    ${comment.content}
                </div>
            </div>
        `;
        
        $('#comments-container').prepend(commentHtml);
    }

    // Format date and time for display
    function formatDateTime(date) {
        return date.toLocaleString();
    }

    // Show notification messages
    function showNotification(message, type) {
        // Create notification element if it doesn't exist
        if ($('#notification-container').length === 0) {
            $('body').append('<div id="notification-container" style="position: fixed; top: 20px; right: 20px; z-index: 9999;"></div>');
        }
        
        // Create notification
        var notification = $(`
            <div class="toast" role="alert" aria-live="assertive" aria-atomic="true" data-delay="5000">
                <div class="toast-header bg-${type === 'success' ? 'success' : 'danger'} text-white">
                    <strong class="mr-auto">${type === 'success' ? 'Success' : 'Error'}</strong>
                    <button type="button" class="ml-2 mb-1 close" data-dismiss="toast" aria-label="Close">
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div>
                <div class="toast-body">
                    ${message}
                </div>
            </div>
        `);
        
        // Add to container and show
        $('#notification-container').append(notification);
        notification.toast('show');
        
        // Remove after hiding
        notification.on('hidden.bs.toast', function() {
            $(this).remove();
        });
    }

    // Initialize ticket data table with search and sort
    if ($('#tickets-table').length) {
        $('#tickets-table').DataTable({
            "order": [[0, "desc"]], // Sort by ID descending by default
            "pageLength": 25,
            "responsive": true
        });
    }
});
