#include <opencv2/opencv.hpp>

extern "C" {
    void ProcessEyeFrame(unsigned char* rawData, int width, int height) {
        cv::Mat frame(height, width, CV_8UC4, rawData);

        cv::Mat gray;
        
        cv::cvtColor(frame, gray, cv::COLOR_RGBA2GRAY);
        
        cv::cvtColor(gray, frame, cv::COLOR_GRAY2RGBA);

        // if image flipped, uncomment
        // cv::flip(frame, frame, 0); 
    }
}