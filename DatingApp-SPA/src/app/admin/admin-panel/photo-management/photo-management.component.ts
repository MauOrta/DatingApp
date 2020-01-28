import { Component, OnInit } from '@angular/core';
import { AuthService } from 'src/app/_services/auth.service';
import { AdminService } from 'src/app/_services/admin.service';
import { AlertifyService } from 'src/app/_services/alertify.service';
import { PhotoForModeration } from 'src/app/_models/photoForModeration';

@Component({
  selector: 'app-photo-management',
  templateUrl: './photo-management.component.html',
  styleUrls: ['./photo-management.component.css']
})
export class PhotoManagementComponent implements OnInit {
  photos: PhotoForModeration[];
  constructor(private authService: AuthService,
   private adminService: AdminService, private alertify: AlertifyService) { }

  ngOnInit() {
    this.getPhotosForModeration();
  }

  getPhotosForModeration() {
    this.adminService.getPhotosForModeration().subscribe((photos: PhotoForModeration[]) => {
      this.photos = photos;
      console.log(photos);
    }, error => {
      this.alertify.error(error);
    });
  }

  approvePhotoForModeration(photo: PhotoForModeration) {
    this.adminService.approvePhotoForModeration(photo.userId, photo.id).subscribe(() => {
      this.alertify.success('Photo has been approved');
      this.getPhotosForModeration();
    }, error => {
      this.alertify.error(error);
    });
  }

  rejectPhptpForModeration(photo: PhotoForModeration) {
    this.adminService.rejectPhotoForModeration(photo.userId, photo.id).subscribe(() => {
      this.alertify.success('Photo has been Rejected');
      this.getPhotosForModeration();
    }, error => {
      this.alertify.error(error);
    });
  }
}
